namespace Nexora.Contracts.Auth;

/// <summary>
/// Corpo de POST /v1/auth/authorize (edge) — porta de authorizeRequestSchema. É a elevação
/// pontual do ADR-023: o gerente informa o próprio PIN para autorizar a ação do operador logado
/// no terminal, sem trocar de sessão.
/// </summary>
public sealed record AuthorizeSensitiveActionRequest(
    string Action,
    string Pin,
    IReadOnlyDictionary<string, object?> Context);

/// <summary>Resposta de autorização pontual — porta de AuthorizationResponseDto (authorization.controller.ts).</summary>
public sealed record AuthorizeSensitiveActionResponse(
    string AuthorizationToken,
    int ExpiresIn,
    AuthorizedBySummary AuthorizedBy);
