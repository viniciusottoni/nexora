namespace Nexora.Shared.Errors;

/// <summary>
/// Códigos de erro da US-155 (Proprietários, usuários iniciais e convites) — ADR-021. Arquivo
/// próprio (não editar <c>ApiErrorCodes.Tenants.cs</c>), mesma disciplina de coordenação entre
/// agentes já usada por <c>ApiErrorCodes.Plans.cs</c> (US-154).
/// </summary>
/// <remarks>
/// Nenhum destes códigos está (ainda) no <c>switch</c> de <c>ResultExtensions.MapErrorCode</c> —
/// esse arquivo é hotspot de integração central (ver docstring dele: "não editar em paralelo por
/// múltiplos agentes"). Até essa integração acontecer, todos os códigos abaixo caem no catch-all
/// (500) em vez do status HTTP documentado ao lado de cada um — mesma lacuna já documentada e
/// aceita pela US-154 (<c>ApiErrorCodes.PlanNotAvailable</c>/<c>TenantPlanResultMappingTests</c>).
/// <see cref="Nexora.Shared.Errors.ApiErrorCodes.ReasonRequired"/> (definido em
/// <c>ApiErrorCodes.Tenants.cs</c>, US-153) É reaproveitado por esta US para "motivo obrigatório" —
/// já está mapeado para 422 no switch central, então os testes que o exercitam NÃO ficam vermelhos.
/// </remarks>
public static partial class ApiErrorCodes
{
    /// <summary>Tenant sem proprietário resolvido (nenhum <c>user_role</c> com papel OWNER) — 404.</summary>
    public const string OwnershipOwnerNotFound = "OWNERSHIP_OWNER_NOT_FOUND";

    /// <summary>Reenvio/correção de convite pedido para um proprietário que já aceitou (tem senha) — 422.</summary>
    public const string OwnershipOwnerAlreadyActive = "OWNERSHIP_OWNER_ALREADY_ACTIVE";

    /// <summary>Novo e-mail já pertence a outro <c>AppUser</c> (qualquer tenant) — 422.</summary>
    public const string OwnershipEmailAlreadyInUse = "OWNERSHIP_EMAIL_ALREADY_IN_USE";

    /// <summary>Convite inexistente OU pertencente a outro tenant (RN-015 — nunca revela qual dos dois) — 404.</summary>
    public const string OwnershipInviteNotFound = "OWNERSHIP_INVITE_NOT_FOUND";

    /// <summary>Tentativa de revogar convite já aceito — 422.</summary>
    public const string OwnershipInviteAlreadyConsumed = "OWNERSHIP_INVITE_ALREADY_CONSUMED";

    /// <summary>Tentativa de revogar convite já revogado — 422.</summary>
    public const string OwnershipInviteAlreadyRevoked = "OWNERSHIP_INVITE_ALREADY_REVOKED";

    /// <summary>Usuário-alvo da transferência não encontrado NESTE tenant (RN-015 — nunca revela se existe em outro) — 404.</summary>
    public const string OwnershipTargetUserNotFound = "OWNERSHIP_TARGET_USER_NOT_FOUND";

    /// <summary>Transferência para o próprio proprietário atual — 422.</summary>
    public const string OwnershipSameOwner = "OWNERSHIP_SAME_OWNER";

    /// <summary>Tenant sem papel OWNER configurado — inconsistência de dado, tratada como falha de negócio (422), nunca 500 silencioso.</summary>
    public const string OwnershipNoOwnerRoleConfigured = "OWNERSHIP_NO_OWNER_ROLE_CONFIGURED";

    /// <summary>Desbloqueio pedido para um proprietário que não está bloqueado — 422.</summary>
    public const string OwnershipOwnerNotBlocked = "OWNERSHIP_OWNER_NOT_BLOCKED";
}
