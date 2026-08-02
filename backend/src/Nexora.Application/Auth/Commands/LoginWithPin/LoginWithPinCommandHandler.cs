using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Auth.Shared;
using Nexora.Contracts.Auth;
using Nexora.Domain.Common;
using Nexora.Domain.Metrics;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Auth.Commands.LoginWithPin;

/// <summary>
/// Porta 1:1 de AuthenticationService.loginWithPin + PrismaAuthRepository
/// (packages/domain/src/auth/authentication-service.ts, apps/api-edge/src/modules/auth/prisma-auth.repository.ts).
/// Bloqueio de 5 tentativas / 15 min é por dispositivo (<see cref="AuthAttempt"/>), não por
/// usuário — o PIN sozinho não identifica quem está tentando até o candidato ser encontrado.
/// </summary>
internal sealed class LoginWithPinCommandHandler : IRequestHandler<LoginWithPinCommand, Result<PinLoginResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly ICredentialHasher _credentialHasher;
    private readonly IPinLookupDigester _pinLookup;
    private readonly ISecretDigester _secretDigester;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly ILogger<LoginWithPinCommandHandler> _logger;

    public LoginWithPinCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        ICredentialHasher credentialHasher,
        IPinLookupDigester pinLookup,
        ISecretDigester secretDigester,
        ITokenIssuer tokenIssuer,
        ILogger<LoginWithPinCommandHandler> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _credentialHasher = credentialHasher;
        _pinLookup = pinLookup;
        _secretDigester = secretDigester;
        _tokenIssuer = tokenIssuer;
        _logger = logger;
    }

    public async Task<Result<PinLoginResponse>> Handle(LoginWithPinCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is not { } tenantId)
        {
            return Result<PinLoginResponse>.Failure(
                "Não foi possível identificar a instalação deste terminal.",
                ApiErrorCodes.TenantContextMissing);
        }

        var now = DateTimeOffset.UtcNow;

        if (string.IsNullOrWhiteSpace(request.DeviceSecret))
        {
            // Porta fiel do controller TS: ausência do header X-Device-Secret é rejeitada antes de
            // qualquer consulta, sem registrar notificação ao gerente (isso só ocorre dentro do
            // fluxo de autenticação, quando o dispositivo é consultado e não é encontrado/ativo).
            return Result<PinLoginResponse>.Failure(AuthErrorMessages.DeviceNotRegistered, ApiErrorCodes.AuthDeviceNotRegistered);
        }

        var device = await _db.Devices
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId && d.TenantId == tenantId, cancellationToken);

        if (device is null || !device.IsActive || !VerifyDeviceSecret(device, request.DeviceSecret))
        {
            NotifyManager(tenantId, request.DeviceId, "DEVICE_NOT_REGISTERED", now);
            _logger.LogWarning("Tentativa de login com dispositivo não autorizado. DeviceId={DeviceId}", request.DeviceId);
            return Result<PinLoginResponse>.Failure(AuthErrorMessages.DeviceNotRegistered, ApiErrorCodes.AuthDeviceNotRegistered);
        }

        var attempt = await _db.AuthAttempts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.DeviceId == device.Id, cancellationToken);
        var isNewAttempt = attempt is null;
        attempt ??= AuthAttempt.Create(tenantId, device.Id);

        if (!isNewAttempt && attempt.IsBlocked(now))
        {
            var retryAfter = LockoutPolicy.RetryAfterSeconds(attempt.BlockedUntil, now);
            return Result<PinLoginResponse>.Failure(AuthErrorMessages.PinLocked, ApiErrorCodes.AuthPinLocked, RetryAfterErrors(retryAfter));
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

        var authenticated = candidate is not null && _credentialHasher.Verify(candidate.PinHash!, request.Pin)
            ? candidate
            : null;

        if (authenticated is null)
        {
            attempt.RecordFailure();
            if (attempt.FailedAttempts >= LockoutPolicy.MaxFailedAttempts)
            {
                attempt.Block(now.AddMinutes(LockoutPolicy.LockMinutes));
                NotifyManager(tenantId, device.Id, "PIN_LOCKED", now, device.StoreId);
            }
            if (isNewAttempt)
            {
                _db.AuthAttempts.Add(attempt);
            }

            return Result<PinLoginResponse>.Failure(AuthErrorMessages.InvalidCredentials, ApiErrorCodes.AuthInvalidCredentials);
        }

        attempt.Reset();
        if (isNewAttempt)
        {
            _db.AuthAttempts.Add(attempt);
        }

        device.MarkSeen();

        var roles = authenticated.UserRoles
            .Select(ur => ur.Role.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var permissions = authenticated.UserRoles
            .SelectMany(ur => PermissionsJson.Parse(ur.Role.Permissions))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var sessionId = IdGenerator.NewId();
        var claims = new AccessClaims(
            Subject: authenticated.Id,
            TenantId: tenantId,
            StoreId: device.StoreId,
            Roles: roles,
            Permissions: permissions,
            DeviceId: device.Id,
            SessionId: sessionId);

        var accessToken = await _tokenIssuer.IssueAccessTokenAsync(claims, AuthTokenTtlSeconds.PinAccess, cancellationToken);

        _db.AuthSessions.Add(AuthSession.Create(
            tenantId,
            authenticated.Id,
            device.Id,
            refreshHash: null,
            expiresAt: now.AddSeconds(AuthTokenTtlSeconds.PinAccess)));

        authenticated.RecordSuccessfulLogin();

        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId: tenantId,
            type: "user.authenticated",
            aggregateType: nameof(AppUser),
            aggregateId: authenticated.Id,
            payload: JsonSerializer.Serialize(new { method = "PIN", deviceId = device.Id }),
            origin: "EDGE",
            occurredAt: now,
            storeId: device.StoreId,
            actorId: authenticated.Id,
            deviceId: device.Id));

        _logger.LogInformation(
            "Login por PIN bem-sucedido. TenantId={TenantId} UserId={UserId} DeviceId={DeviceId}",
            tenantId, authenticated.Id, device.Id);

        return Result<PinLoginResponse>.Success(new PinLoginResponse(
            accessToken,
            new AuthenticatedUserSummary(authenticated.Id, authenticated.Name),
            permissions));
    }

    private bool VerifyDeviceSecret(Device device, string secret)
    {
        if (string.IsNullOrEmpty(device.SecretHash))
        {
            return false;
        }

        var expected = _secretDigester.Digest(secret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(device.SecretHash));
    }

    /// <summary>
    /// US-004, gap "notificação ao gestor inconsistente" — PIN_LOCKED/DEVICE_NOT_REGISTERED só
    /// gravavam <see cref="AuditLog"/> (rastro passivo, ninguém é avisado), diferente de
    /// PERMISSION_CHANGED (que já cria <see cref="Alert"/> de verdade — ver
    /// CreateRoleCommandHandler/UpdateRoleCommandHandler). Mesmo padrão daqui em diante: todo
    /// evento que precisa de atenção do gestor grava os dois, na mesma transação (ADR-006).
    /// <paramref name="storeId"/> é opcional porque DEVICE_NOT_REGISTERED pode disparar antes de
    /// qualquer <c>Device</c> ser encontrado (device inexistente/inativo) — não há loja para
    /// vincular ainda; PIN_LOCKED sempre tem um dispositivo resolvido, e chama isto com
    /// <c>device.StoreId</c>.
    /// </summary>
    private void NotifyManager(Guid tenantId, Guid deviceId, string type, DateTimeOffset now, Guid? storeId = null)
    {
        _db.AuditLogs.Add(AuditLog.Create(tenantId, type, "device", now, storeId: storeId, deviceId: deviceId, entityId: deviceId));

        _db.Alerts.Add(Alert.Create(
            tenantId,
            type: type,
            message: DescribeManagerAlert(type),
            storeId: storeId,
            targetRoles: new[] { "OWNER", "MANAGER" },
            payload: JsonSerializer.Serialize(new { deviceId })));
    }

    private static string DescribeManagerAlert(string type) => type switch
    {
        "DEVICE_NOT_REGISTERED" => "Tentativa de login com dispositivo não autorizado.",
        "PIN_LOCKED" => "Acesso bloqueado por excesso de tentativas de PIN incorretas.",
        _ => $"Evento de autenticação: {type}.",
    };

    private static IReadOnlyDictionary<string, string[]> RetryAfterErrors(int retryAfterSeconds) =>
        new Dictionary<string, string[]> { ["retryAfterSeconds"] = new[] { retryAfterSeconds.ToString(CultureInfo.InvariantCulture) } };
}
