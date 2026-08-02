namespace Nexora.Contracts.Tenants;

/// <summary>Corpo de <c>POST /v1/platform/tenants</c> — espelha <c>CreateTenantDto</c> do NestJS original.</summary>
public sealed record ProvisionTenantOwnerRequest(string Name, string Email);

public sealed record ProvisionTenantStoreRequest(string Name, string Timezone);

public sealed record ProvisionTenantRequest(
    string Name,
    string Slug,
    string Plan,
    string Template,
    ProvisionTenantOwnerRequest Owner,
    ProvisionTenantStoreRequest Store);
