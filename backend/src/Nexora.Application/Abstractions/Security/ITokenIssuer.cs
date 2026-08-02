namespace Nexora.Application.Abstractions.Security;

/// <summary>Claims de acesso — mesmo formato usado no access/refresh token (edge e cloud).</summary>
public sealed record AccessClaims(
    Guid Subject,
    Guid TenantId,
    Guid StoreId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    Guid? DeviceId = null,
    Guid? SessionId = null,
    bool Mfa = false);

/// <summary>Emissão de JWT (implementado em Infrastructure com System.IdentityModel.Tokens.Jwt).</summary>
public interface ITokenIssuer
{
    Task<string> IssueAccessTokenAsync(AccessClaims claims, int ttlSeconds, CancellationToken cancellationToken = default);

    Task<string> IssueRefreshTokenAsync(AccessClaims claims, int ttlSeconds, CancellationToken cancellationToken = default);

    /// <summary>Token de autorização pontual (elevação de permissão — ADR-023).</summary>
    Task<string> IssueAuthorizationTokenAsync(IReadOnlyDictionary<string, object> claims, int ttlSeconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Token anônimo de escopo mínimo do cliente na mesa (US-021 §3.1/§7: "Emissão de token
    /// anônimo de sessão, com escopo mínimo e expiração junto da sessão da mesa"). Diferente de
    /// <see cref="IssueAccessTokenAsync"/> (sem <c>roles</c>/<c>perms</c> — este token nunca deve
    /// satisfazer uma policy de permissão de staff) e de <see cref="IssueAuthorizationTokenAsync"/>
    /// (semântica distinta — elevação pontual de uma ação já autenticada, não uma sessão pública
    /// anônima). Carrega só o necessário para escopar as próprias ações do cliente na mesa (ver
    /// cardápio, chamar garçom, pedir a conta — RN-015, "nunca leitura de outra mesa"):
    /// <c>ses</c>=id da <c>table_session</c>, <c>tbl</c>=id da <c>dining_table</c>,
    /// <c>tid</c>=tenant. Validado por <see cref="ValidateTableSessionTokenAsync"/>, nunca pelos
    /// validadores de refresh/autorização.
    /// </summary>
    Task<string> IssueTableSessionTokenAsync(Guid sessionId, Guid tenantId, Guid tableId, int ttlSeconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida um token de sessão de mesa emitido por <see cref="IssueTableSessionTokenAsync"/> e
    /// devolve suas claims. Lança <see cref="Microsoft.IdentityModel.Tokens.SecurityTokenException"/>
    /// quando o token é inválido, expirado, ou não é um token deste tipo — mesma família de exceção
    /// dos demais validadores desta interface.
    /// </summary>
    Task<TableSessionTokenClaims> ValidateTableSessionTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida a assinatura/expiração de um refresh token e devolve suas claims — porta de
    /// <c>JwtTokenService.verify(token, 'refresh')</c> (apps/api-cloud/src/modules/auth/jwt-token.service.ts).
    /// Lança exceção quando o token é inválido, expirado, ou não é um refresh token — o handler
    /// converte qualquer falha em <c>AUTH_INVALID_CREDENTIALS</c> (mesma política do TS original,
    /// que nunca distingue o motivo exato ao cliente).
    /// </summary>
    Task<RefreshTokenClaims> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida a assinatura/expiração (120s — <see cref="AuthTokenTtlSeconds.Authorization"/>) de um
    /// token de autorização pontual (ADR-023, <see cref="IssueAuthorizationTokenAsync"/>) e devolve
    /// suas claims — contraparte de leitura que faltava para o header <c>X-Authorization-Token</c>
    /// ser efetivamente validado (US-004, gap "autorização pontual é só emitida, nunca validada").
    /// Lança <see cref="Microsoft.IdentityModel.Tokens.SecurityTokenException"/> quando o token é
    /// inválido, expirado ou não é um token de autorização (mesma família de exceção de
    /// <see cref="ValidateRefreshTokenAsync"/>) — quem chama decide a mensagem/código expostos.
    /// </summary>
    Task<AuthorizationTokenClaims> ValidateAuthorizationTokenAsync(string token, CancellationToken cancellationToken = default);
}

/// <summary>Claims decodificadas de um refresh token válido.</summary>
public sealed record RefreshTokenClaims(
    Guid Subject,
    Guid TenantId,
    Guid StoreId,
    Guid SessionId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    bool Mfa);

/// <summary>
/// Claims decodificadas de um token de autorização pontual válido (ADR-023) — espelha exatamente
/// o dicionário montado por <c>AuthorizeSensitiveActionCommandHandler</c> ao emitir o token
/// (<c>sub</c>=ator que pediu a elevação, <c>tid</c>/<c>sid</c>/<c>did</c>=contexto do terminal,
/// <c>action</c>=ação sensível autorizada, <c>contextHash</c>=hash estável do contexto da ação,
/// <c>authorizedBy</c>=quem informou o PIN de gerente).
/// </summary>
public sealed record AuthorizationTokenClaims(
    Guid ActorId,
    Guid TenantId,
    Guid StoreId,
    Guid? DeviceId,
    string Action,
    string ContextHash,
    Guid AuthorizedBy);

/// <summary>Claims decodificadas de um token de sessão de mesa válido (US-021) — ver <see cref="ITokenIssuer.IssueTableSessionTokenAsync"/>.</summary>
public sealed record TableSessionTokenClaims(Guid SessionId, Guid TenantId, Guid TableId);

/// <summary>TTLs padrão de token — mesmos valores usados na versão TypeScript (packages/domain/src/auth/authorization.ts).</summary>
public static class AuthTokenTtlSeconds
{
    public const int PasswordAccess = 15 * 60;
    public const int Refresh = 30 * 24 * 60 * 60;
    public const int PinAccess = 8 * 60 * 60;
    public const int Authorization = 120;

    /// <summary>
    /// US-021 §3.1: "expiração junto da sessão da mesa". Sem um limite de tempo de refeição
    /// modelado (não existe hoje um campo de "duração máxima esperada" em <c>TenantConfig</c>),
    /// 6h cobre generosamente qualquer almoço/jantar — bem mais longo que <see cref="PinAccess"/>
    /// porque o cliente não deve precisar ler o QR de novo no meio da refeição. Renovado
    /// naturalmente a cada nova leitura do QR (mesma sessão, novo token) enquanto a mesa continuar
    /// aberta.
    /// </summary>
    public const int TableSession = 6 * 60 * 60;
}
