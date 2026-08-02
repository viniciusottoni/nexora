namespace Nexora.Application.Abstractions.Security;

/// <summary>
/// Tenant nunca vem do cliente em rota autenticada — sempre do token, resolvido aqui
/// (doc. 05; ver ADR-004). Implementado em Infrastructure a partir do HttpContext/JWT
/// (Api.Cloud) ou da identidade fixa do servidor local (Api.Edge, ADR-004 "uma loja =
/// um tenant" no edge).
/// </summary>
public interface ICurrentTenantContext
{
    /// <summary>Nulo fora de contexto autenticado (ex.: endpoints públicos de login).</summary>
    Guid? TenantId { get; }

    Guid? StoreId { get; }

    Guid? UserId { get; }

    IReadOnlyList<string> Roles { get; }

    IReadOnlyList<string> Permissions { get; }

    /// <summary>Id do dispositivo autenticado (edge, login por PIN) — nulo no cloud.</summary>
    Guid? DeviceId { get; }

    /// <summary>Id da sessão de autenticação corrente (claim <c>ses</c>) — usado em auditoria e revogação.</summary>
    Guid? SessionId { get; }
}
