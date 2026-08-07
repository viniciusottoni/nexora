namespace Nexora.Shared.Errors;

/// <summary>Códigos de erro do módulo de provisionamento de tenants (ADR-021).</summary>
public static partial class ApiErrorCodes
{
    public const string TenantNotFound = "TENANT_NOT_FOUND";

    /// <summary>
    /// Mapeia para 422 em <c>ResultExtensions.MapStatusCode</c> (entrada explícita — não é bem um
    /// conflito de concorrência, é uma violação de regra de negócio, mesma leitura da versão TS
    /// <c>SlugAlreadyTakenError</c>). Valor corrigido de "TENANT_SLUG_ALREADY_TAKEN" para
    /// "SLUG_ALREADY_TAKEN" para bater com o contrato documentado na US-002 e com
    /// <c>errorCodeSchema</c> em packages/contracts/src/errors.ts (o frontend — ex.
    /// provision-tenant-page.tsx — já comparava contra "SLUG_ALREADY_TAKEN").
    /// </summary>
    public const string TenantSlugAlreadyTaken = "SLUG_ALREADY_TAKEN";

    /// <summary>
    /// Único código para convite consumido, expirado ou inexistente — mesma decisão da função
    /// <c>accept_owner_invite</c> do TS: não vazar qual dos três motivos ocorreu (evita enumeração
    /// de convites válidos). Contém "INVALID_CREDENTIALS" de propósito para mapear a 401 pela
    /// convenção já existente em ResultExtensions.
    /// </summary>
    public const string OwnerInviteInvalidCredentials = "OWNER_INVITE_INVALID_CREDENTIALS";

    // US-153 · Ciclo de vida do estabelecimento — POST /v1/platform/tenants/{id}/status-transitions.
    /// <summary>Alvo não alcançável a partir do status atual (matriz canônica em <c>Nexora.Domain.Platform.TenantStatusTransitions</c>) — 409, doc §4 cenário "Transição inválida".</summary>
    public const string TenantStatusTransitionInvalid = "TENANT_STATUS_TRANSITION_INVALID";

    /// <summary><c>If-Match</c> não bate com <c>tenant.status_version</c> corrente — 409, doc §4 cenário "Concorrência".</summary>
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";

    /// <summary>Motivo ausente/vazio — 422, doc §7 (toda transição administrativa exige motivo substantivo).</summary>
    public const string ReasonRequired = "REASON_REQUIRED";
}
