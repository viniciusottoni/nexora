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
}
