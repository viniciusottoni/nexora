using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Auth.Shared;
using Nexora.Contracts.Auth;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Auth.Commands.RefreshToken;

/// <summary>
/// Porta de RefreshAuthService.execute (apps/api-cloud/src/modules/auth/refresh-auth.service.ts).
/// Qualquer falha (assinatura inválida, sessão não encontrada/revogada/expirada, usuário
/// bloqueado/inativo) converge para a mesma mensagem genérica de credenciais inválidas — o TS
/// original nunca distingue o motivo exato ao cliente (RNF-SEG-15).
/// </summary>
internal sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<PasswordAuthResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly ISecretDigester _secretDigester;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IApplicationDbContext db,
        ITokenIssuer tokenIssuer,
        ISecretDigester secretDigester,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _db = db;
        _tokenIssuer = tokenIssuer;
        _secretDigester = secretDigester;
        _logger = logger;
    }

    public async Task<Result<PasswordAuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        RefreshTokenClaims claims;
        try
        {
            claims = await _tokenIssuer.ValidateRefreshTokenAsync(request.RefreshToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Refresh token inválido ou expirado.");
            return Result<PasswordAuthResponse>.Failure(AuthErrorMessages.InvalidCredentials, ApiErrorCodes.AuthInvalidCredentials);
        }

        var now = DateTimeOffset.UtcNow;

        // A assinatura já garante o tenant de forma confiável — habilita RLS para o restante do
        // fluxo (porta de setTenant(tx, tenantId) em findRefreshSession).
        await _db.SetTenantContextAsync(claims.TenantId, cancellationToken);

        var refreshHash = _secretDigester.Digest(request.RefreshToken);

        var session = await _db.AuthSessions
            .Include(s => s.User).ThenInclude(u => u.Tenant)
            .Include(s => s.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(
                s => s.Id == claims.SessionId &&
                     s.TenantId == claims.TenantId &&
                     s.RefreshHash == refreshHash &&
                     s.RevokedAt == null &&
                     s.ExpiresAt > now,
                cancellationToken);

        if (session is null || session.User.Status != UserStatus.Active || session.User.DeletedAt is not null)
        {
            return Result<PasswordAuthResponse>.Failure(AuthErrorMessages.InvalidCredentials, ApiErrorCodes.AuthInvalidCredentials);
        }

        var storeId = await UserAccessLoader.ResolveStoreAsync(_db, session.User, cancellationToken);
        if (storeId is null)
        {
            return Result<PasswordAuthResponse>.Failure(AuthErrorMessages.InvalidCredentials, ApiErrorCodes.AuthInvalidCredentials);
        }

        var roles = session.User.UserRoles.Select(ur => ur.Role.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var permissions = session.User.UserRoles
            .SelectMany(ur => PermissionsJson.Parse(ur.Role.Permissions))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var newClaims = new AccessClaims(
            Subject: session.UserId,
            TenantId: session.TenantId,
            StoreId: storeId.Value,
            Roles: roles,
            Permissions: permissions,
            SessionId: session.Id,
            Mfa: claims.Mfa);

        var accessToken = await _tokenIssuer.IssueAccessTokenAsync(newClaims, AuthTokenTtlSeconds.PasswordAccess, cancellationToken);
        var nextRefreshToken = await _tokenIssuer.IssueRefreshTokenAsync(newClaims, AuthTokenTtlSeconds.Refresh, cancellationToken);

        session.Rotate(_secretDigester.Digest(nextRefreshToken), now.AddSeconds(AuthTokenTtlSeconds.Refresh));

        _logger.LogInformation(
            "Refresh de token bem-sucedido. TenantId={TenantId} UserId={UserId} SessionId={SessionId}",
            session.TenantId, session.UserId, session.Id);

        return Result<PasswordAuthResponse>.Success(new PasswordAuthResponse(
            accessToken,
            nextRefreshToken,
            new AuthenticatedUserSummary(session.UserId, session.User.Name),
            new AuthenticatedTenantSummary(session.TenantId, session.User.Tenant.Name),
            permissions));
    }
}
