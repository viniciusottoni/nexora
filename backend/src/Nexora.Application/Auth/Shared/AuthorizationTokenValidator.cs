using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Security;
using Nexora.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Auth.Shared;

/// <summary>
/// Implementação de <see cref="IAuthorizationTokenValidator"/> — porta de leitura que faltava para
/// o header <c>X-Authorization-Token</c> emitido por <c>AuthorizeSensitiveActionCommandHandler</c>
/// (ADR-023). Delega a validação de assinatura/expiração/uso a <see cref="ITokenIssuer.ValidateAuthorizationTokenAsync"/>
/// (implementado por <c>JwtTokenIssuer</c> em Infrastructure) e adiciona a regra de negócio que o
/// token isolado não carrega: a ação autorizada precisa ser EXATAMENTE a que está sendo protegida
/// — um token emitido para <c>"ADJUST_STOCK"</c> nunca autoriza <c>"CANCEL_STARTED_ITEM"</c>, ainda
/// que a mesma pessoa/PIN tenha concedido os dois.
/// </summary>
public sealed class AuthorizationTokenValidator : IAuthorizationTokenValidator
{
    private readonly ITokenIssuer _tokenIssuer;
    private readonly ILogger<AuthorizationTokenValidator> _logger;

    public AuthorizationTokenValidator(ITokenIssuer tokenIssuer, ILogger<AuthorizationTokenValidator> logger)
    {
        _tokenIssuer = tokenIssuer;
        _logger = logger;
    }

    public async Task<Result<AuthorizationGrant>> ValidateAsync(
        string? token, string requiredAction, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Deny();
        }

        AuthorizationTokenClaims claims;
        try
        {
            claims = await _tokenIssuer.ValidateAuthorizationTokenAsync(token, cancellationToken);
        }
        catch (Exception ex)
        {
            // Mesma política de ValidateRefreshTokenAsync: assinatura inválida, token expirado
            // (mais de 120s desde a emissão — AuthTokenTtlSeconds.Authorization) ou que não é um
            // token de autorização convergem para a mesma resposta, sem detalhar o motivo ao
            // cliente (RNF-SEG-15).
            _logger.LogWarning(ex, "Token de autorização inválido ou expirado.");
            return Deny();
        }

        if (!string.Equals(claims.Action, requiredAction, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Token de autorização emitido para ação diferente. Esperado={RequiredAction} Token={TokenAction}",
                requiredAction, claims.Action);
            return Deny();
        }

        return Result<AuthorizationGrant>.Success(new AuthorizationGrant(
            AuthorizedBy: claims.AuthorizedBy,
            ActorId: claims.ActorId,
            TenantId: claims.TenantId,
            StoreId: claims.StoreId,
            DeviceId: claims.DeviceId,
            Action: claims.Action,
            ContextHash: claims.ContextHash));
    }

    private static Result<AuthorizationGrant> Deny() =>
        Result<AuthorizationGrant>.Failure(AuthErrorMessages.AuthorizationTokenInvalid, ApiErrorCodes.AuthorizationRequired);
}
