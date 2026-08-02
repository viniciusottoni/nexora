namespace Nexora.Contracts.Tenants;

public sealed record ProvisionedTenantResponse(Guid Id, string Slug, string Status);

public sealed record ProvisionedStoreResponse(Guid Id, string Name);

public sealed record ProvisioningChecklistItemResponse(string Code, string Label, string Status);

/// <summary>Espelha <c>CreateTenantResponseDto</c> do NestJS original.</summary>
public sealed record ProvisionTenantResponse(
    ProvisionedTenantResponse Tenant,
    ProvisionedStoreResponse Store,
    string InstallToken,
    string InstallCommand,
    string OwnerInviteSentTo,
    IReadOnlyList<ProvisioningChecklistItemResponse> Checklist);
