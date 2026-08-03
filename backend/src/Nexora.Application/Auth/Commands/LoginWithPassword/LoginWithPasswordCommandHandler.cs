using System.Globalization;
using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Auth.Shared;
using Nexora.Contracts.Auth;
using Nexora.Domain.Common;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Auth.Commands.LoginWithPassword;

/// <summary>
/// Porta de PasswordAuthenticationService.login + PrismaPasswordAuthRepository.findByEmail
/// (packages/domain/src/auth/password-authentication.ts,
/// apps/api-cloud/src/modules/auth/prisma-password-auth.repository.ts).
/// <para>
/// Extensão deliberada em relação ao TS: o bloqueio de 5 tentativas / 15 min (que no TS só existe
/// para o login por PIN, via <c>AuthAttempt</c>) também é aplicado aqui usando os campos
/// <c>AppUser.FailedAttempts</c>/<c>BlockedUntil</c> — já previstos na entidade e na DDL, mas não
/// exercitados pelo login por senha original. Ver decisão registrada no relatório de porte.
/// </para>
/// </summary>
internal sealed class LoginWithPasswordCommandHandler
    : IRequestHandler<LoginWithPasswordCommand, Result<PasswordAuthResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICredentialHasher _credentialHasher;
    private readonly IOtpVerifier _otpVerifier;
    private readonly IMfaSecretCipher _mfaCipher;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly ISecretDigester _secretDigester;
    private readonly ILogger<LoginWithPasswordCommandHandler> _logger;

    public LoginWithPasswordCommandHandler(
        IApplicationDbContext db,
        ICredentialHasher credentialHasher,
        IOtpVerifier otpVerifier,
        IMfaSecretCipher mfaCipher,
        ITokenIssuer tokenIssuer,
        ISecretDigester secretDigester,
        ILogger<LoginWithPasswordCommandHandler> logger)
    {
        _db = db;
        _credentialHasher = credentialHasher;
        _otpVerifier = otpVerifier;
        _mfaCipher = mfaCipher;
        _tokenIssuer = tokenIssuer;
        _secretDigester = secretDigester;
        _logger = logger;
    }

    public async Task<Result<PasswordAuthResponse>> Handle(LoginWithPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;

        // Ainda sem tenant conhecido: o RLS (ADR-004) nega leitura de app_user, por isso a busca
        // cruza tenants via FindLoginCredentialByEmailAsync (porta de auth_lookup_user()).
        var lookup = await _db.FindLoginCredentialByEmailAsync(normalizedEmail, cancellationToken);
        if (lookup is null)
        {
            return Result<PasswordAuthResponse>.Failure(AuthErrorMessages.InvalidCredentials, ApiErrorCodes.AuthInvalidCredentials);
        }

        // Tenant agora é conhecido — habilita RLS para o restante do fluxo (porta de setTenant(tx, tenantId)).
        await _db.SetTenantContextAsync(lookup.TenantId, cancellationToken);

        var user = await _db.Users
            .Include(u => u.Tenant)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == lookup.UserId && u.TenantId == lookup.TenantId && u.DeletedAt == null, cancellationToken);

        if (user is null)
        {
            return Result<PasswordAuthResponse>.Failure(AuthErrorMessages.InvalidCredentials, ApiErrorCodes.AuthInvalidCredentials);
        }

        if (user.Status == UserStatus.Blocked)
        {
            if (user.BlockedUntil is { } blockedUntil && now < blockedUntil)
            {
                var retryAfter = LockoutPolicy.RetryAfterSeconds(blockedUntil, now);
                return Result<PasswordAuthResponse>.Failure(AuthErrorMessages.UserBlocked, ApiErrorCodes.AuthUserBlocked, RetryAfterErrors(retryAfter));
            }

            user.Unblock();
        }

        // UserStatus.Invited (US-002, Docs/Domain/12 §8): dono recém-provisionado, convite ainda
        // não aceito (OwnerInvite.Consume + AppUser.SetPassword é o único caminho para UserStatus.Active
        // — ver AcceptOwnerInvitationCommandHandler). Mesmo código/mensagem de UserStatus.Inactive:
        // do ponto de vista do login por senha, "convite pendente" e "inativo" são o mesmo resultado
        // (não pode autenticar), e reaproveitar o código evita introduzir um novo em
        // ApiErrorCodes/ResultExtensions.MapStatusCode só para esta distinção.
        if (user.Status == UserStatus.Inactive || user.Status == UserStatus.Invited)
        {
            return Result<PasswordAuthResponse>.Failure(AuthErrorMessages.UserInactive, ApiErrorCodes.AuthUserInactive);
        }

        if (!_credentialHasher.Verify(lookup.PasswordHash, request.Password))
        {
            RegisterFailedAttempt(user, now);
            return Result<PasswordAuthResponse>.Failure(AuthErrorMessages.InvalidCredentials, ApiErrorCodes.AuthInvalidCredentials);
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var requiresMfa = roles.Contains("PLATFORM_ADMIN", StringComparer.OrdinalIgnoreCase) || user.MfaSecret is not null;

        if (requiresMfa)
        {
            var otpValid =
                user.MfaSecret is not null &&
                !string.IsNullOrWhiteSpace(request.Otp) &&
                _otpVerifier.Verify(_mfaCipher.Decrypt(user.MfaSecret), request.Otp!);

            if (!otpValid)
            {
                RegisterFailedAttempt(user, now);
                return Result<PasswordAuthResponse>.Failure(AuthErrorMessages.InvalidCredentials, ApiErrorCodes.AuthInvalidCredentials);
            }
        }

        var storeId = await UserAccessLoader.ResolveStoreAsync(_db, user, cancellationToken);
        if (storeId is null)
        {
            return Result<PasswordAuthResponse>.Failure(AuthErrorMessages.InvalidCredentials, ApiErrorCodes.AuthInvalidCredentials);
        }

        var permissions = user.UserRoles
            .SelectMany(ur => PermissionsJson.Parse(ur.Role.Permissions))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var sessionId = IdGenerator.NewId();
        var claims = new AccessClaims(
            Subject: user.Id,
            TenantId: user.TenantId,
            StoreId: storeId.Value,
            Roles: roles,
            Permissions: permissions,
            SessionId: sessionId,
            Mfa: requiresMfa);

        var accessToken = await _tokenIssuer.IssueAccessTokenAsync(claims, AuthTokenTtlSeconds.PasswordAccess, cancellationToken);
        var refreshToken = await _tokenIssuer.IssueRefreshTokenAsync(claims, AuthTokenTtlSeconds.Refresh, cancellationToken);

        _db.AuthSessions.Add(AuthSession.Create(
            user.TenantId,
            user.Id,
            deviceId: null,
            refreshHash: _secretDigester.Digest(refreshToken),
            expiresAt: now.AddSeconds(AuthTokenTtlSeconds.Refresh),
            id: sessionId));

        user.RecordSuccessfulLogin();

        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId: user.TenantId,
            type: "user.authenticated",
            aggregateType: nameof(AppUser),
            aggregateId: user.Id,
            payload: JsonSerializer.Serialize(new { method = "PASSWORD" }),
            origin: "CLOUD",
            occurredAt: now,
            storeId: storeId.Value,
            actorId: user.Id));

        _logger.LogInformation(
            "Login por senha bem-sucedido. TenantId={TenantId} UserId={UserId}", user.TenantId, user.Id);

        return Result<PasswordAuthResponse>.Success(new PasswordAuthResponse(
            accessToken,
            refreshToken,
            new AuthenticatedUserSummary(user.Id, user.Name),
            new AuthenticatedTenantSummary(user.TenantId, user.Tenant.Name),
            permissions));
    }

    private static void RegisterFailedAttempt(AppUser user, DateTimeOffset now)
    {
        user.RecordFailedLogin();
        if (user.FailedAttempts >= LockoutPolicy.MaxFailedAttempts)
        {
            user.Block(now.AddMinutes(LockoutPolicy.LockMinutes));
        }
    }

    private static IReadOnlyDictionary<string, string[]> RetryAfterErrors(int retryAfterSeconds) =>
        new Dictionary<string, string[]> { ["retryAfterSeconds"] = new[] { retryAfterSeconds.ToString(CultureInfo.InvariantCulture) } };
}
