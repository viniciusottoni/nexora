using System.Globalization;
using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Auth.Shared;
using Nexora.Contracts.Auth;
using Nexora.Domain.Metrics;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Auth.Commands.AuthorizeSensitiveAction;

/// <summary>
/// Porta de SensitiveAuthorizationService.authorize
/// (apps/api-edge/src/modules/auth/sensitive-authorization.service.ts). O bloqueio de tentativas
/// é o mesmo <see cref="AuthAttempt"/> por dispositivo usado no login por PIN — autorizar também
/// exige um PIN correto no mesmo terminal.
/// </summary>
internal sealed class AuthorizeSensitiveActionCommandHandler
    : IRequestHandler<AuthorizeSensitiveActionCommand, Result<AuthorizeSensitiveActionResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly ICredentialHasher _credentialHasher;
    private readonly IPinLookupDigester _pinLookup;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly ILogger<AuthorizeSensitiveActionCommandHandler> _logger;

    public AuthorizeSensitiveActionCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        ICredentialHasher credentialHasher,
        IPinLookupDigester pinLookup,
        ITokenIssuer tokenIssuer,
        ILogger<AuthorizeSensitiveActionCommandHandler> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _credentialHasher = credentialHasher;
        _pinLookup = pinLookup;
        _tokenIssuer = tokenIssuer;
        _logger = logger;
    }

    public async Task<Result<AuthorizeSensitiveActionResponse>> Handle(
        AuthorizeSensitiveActionCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is not { } tenantId ||
            _tenantContext.StoreId is not { } storeId ||
            _tenantContext.UserId is not { } actorId ||
            _tenantContext.DeviceId is not { } deviceId)
        {
            return Result<AuthorizeSensitiveActionResponse>.Failure(
                AuthErrorMessages.AuthorizationContextInvalid, ApiErrorCodes.AuthorizationContextInvalid);
        }

        if (!SensitiveActionCatalog.ActionPermissions.TryGetValue(request.Action, out var requiredPermission))
        {
            return Result<AuthorizeSensitiveActionResponse>.Failure(
                AuthErrorMessages.PermissionDenied, ApiErrorCodes.AuthPermissionDenied);
        }

        var now = DateTimeOffset.UtcNow;

        var attempt = await _db.AuthAttempts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.DeviceId == deviceId, cancellationToken);
        var isNewAttempt = attempt is null;
        attempt ??= AuthAttempt.Create(tenantId, deviceId);

        if (!isNewAttempt && attempt.IsBlocked(now))
        {
            var retryAfter = LockoutPolicy.RetryAfterSeconds(attempt.BlockedUntil, now);
            return Result<AuthorizeSensitiveActionResponse>.Failure(
                AuthErrorMessages.PinLocked, ApiErrorCodes.AuthPinLocked, RetryAfterErrors(retryAfter));
        }

        var pinLookupDigest = _pinLookup.Digest(request.Pin);

        var candidate = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(
                u => u.TenantId == tenantId &&
                     u.PinLookup == pinLookupDigest &&
                     u.Status == UserStatus.Active &&
                     u.PinHash != null &&
                     u.DeletedAt == null,
                cancellationToken);

        var candidatePermissions = candidate?.UserRoles
            .SelectMany(ur => PermissionsJson.Parse(ur.Role.Permissions))
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? Array.Empty<string>();

        var authorizer = candidate is not null &&
                          PermissionEvaluator.HasPermission(candidatePermissions, requiredPermission) &&
                          _credentialHasher.Verify(candidate.PinHash!, request.Pin)
            ? candidate
            : null;

        if (authorizer is null)
        {
            attempt.RecordFailure();
            if (attempt.FailedAttempts >= LockoutPolicy.MaxFailedAttempts)
            {
                attempt.Block(now.AddMinutes(LockoutPolicy.LockMinutes));
                NotifyManager(tenantId, deviceId, "PIN_LOCKED", now, storeId);
            }
            if (isNewAttempt)
            {
                _db.AuthAttempts.Add(attempt);
            }

            return Result<AuthorizeSensitiveActionResponse>.Failure(
                AuthErrorMessages.InvalidCredentials, ApiErrorCodes.AuthInvalidCredentials);
        }

        attempt.Reset();
        if (isNewAttempt)
        {
            _db.AuthAttempts.Add(attempt);
        }

        var contextHash = AuthorizationContextHasher.Hash(request.Context);

        var authorizationClaims = new Dictionary<string, object>
        {
            ["sub"] = actorId.ToString(),
            ["tid"] = tenantId.ToString(),
            ["sid"] = storeId.ToString(),
            ["did"] = deviceId.ToString(),
            ["action"] = request.Action,
            ["contextHash"] = contextHash,
            ["authorizedBy"] = authorizer.Id.ToString(),
        };

        var authorizationToken = await _tokenIssuer.IssueAuthorizationTokenAsync(
            authorizationClaims, AuthTokenTtlSeconds.Authorization, cancellationToken);

        // EVT-071 authorization.granted — criado ANTES do AuditLog para correlacionar via
        // DomainEventId (E-09/US-090, "Correlação com o evento").
        var authorizationGrantedEvent = DomainEvent.Create(
            tenantId: tenantId,
            type: "authorization.granted",
            aggregateType: nameof(AppUser),
            aggregateId: actorId,
            payload: JsonSerializer.Serialize(new
            {
                action = request.Action,
                authorizedBy = authorizer.Id,
                context = request.Context,
            }),
            origin: "EDGE",
            occurredAt: now,
            storeId: storeId,
            actorId: actorId,
            authorizedBy: authorizer.Id,
            deviceId: deviceId);
        _db.DomainEvents.Add(authorizationGrantedEvent);

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId: tenantId,
            action: request.Action,
            entity: "authorization",
            occurredAt: now,
            storeId: storeId,
            actorId: actorId,
            authorizedBy: authorizer.Id,
            deviceId: deviceId,
            after: JsonSerializer.Serialize(request.Context),
            domainEventId: authorizationGrantedEvent.Id));

        _logger.LogInformation(
            "Autorização concedida. TenantId={TenantId} Action={Action} ActorId={ActorId} AuthorizedBy={AuthorizedBy}",
            tenantId, request.Action, actorId, authorizer.Id);

        return Result<AuthorizeSensitiveActionResponse>.Success(new AuthorizeSensitiveActionResponse(
            authorizationToken,
            AuthTokenTtlSeconds.Authorization,
            new AuthorizedBySummary(authorizer.Id, authorizer.Name)));
    }

    /// <summary>US-004, gap "notificação ao gestor inconsistente" — ver docstring espelhada em <c>LoginWithPinCommandHandler.NotifyManager</c>.</summary>
    private void NotifyManager(Guid tenantId, Guid deviceId, string type, DateTimeOffset now, Guid? storeId = null)
    {
        _db.AuditLogs.Add(AuditLog.Create(tenantId, type, "device", now, storeId: storeId, deviceId: deviceId, entityId: deviceId));

        _db.Alerts.Add(Alert.Create(
            tenantId,
            type: type,
            message: "Acesso à autorização pontual bloqueado por excesso de tentativas de PIN incorretas.",
            storeId: storeId,
            targetRoles: new[] { "OWNER", "MANAGER" },
            payload: JsonSerializer.Serialize(new { deviceId })));
    }

    private static IReadOnlyDictionary<string, string[]> RetryAfterErrors(int retryAfterSeconds) =>
        new Dictionary<string, string[]> { ["retryAfterSeconds"] = new[] { retryAfterSeconds.ToString(CultureInfo.InvariantCulture) } };
}
