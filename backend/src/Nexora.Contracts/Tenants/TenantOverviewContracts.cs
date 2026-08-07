namespace Nexora.Contracts.Tenants;

/// <summary>
/// US-152 · Visão 360 e acesso aos módulos do estabelecimento — porta de
/// <c>GET /v1/platform/tenants/{id}/overview</c>. Espelha
/// <c>packages/contracts/src/tenant-overview.ts</c> (frontend); as duas listas precisam continuar em
/// sincronia. Agrega só metadados administrativos (RN-015) — nunca dado operacional do cliente.
/// </summary>
/// <param name="StatusVersion">US-153 — controle de concorrência otimista; ecoado pelo cliente no header <c>If-Match</c> de <c>POST .../status-transitions</c>.</param>
/// <param name="AvailableTransitions">US-153 — próximos status que o administrador pode escolher AGORA (<c>TenantStatusTransitions.AdminTargetsFrom</c>) — única fonte de verdade da máquina de estados; o frontend nunca reimplementa esta matriz.</param>
public sealed record TenantOverviewTenantResponse(
    Guid Id,
    string Name,
    string Slug,
    string Status,
    string Plan,
    string? Template,
    string? Domain,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int StatusVersion,
    IReadOnlyList<string> AvailableTransitions);

/// <summary><see cref="InviteStatus"/>: <c>"PENDING"</c> | <c>"ACCEPTED"</c> | <c>"EXPIRED"</c>.</summary>
public sealed record TenantOverviewOwnerResponse(string Name, string Email, string InviteStatus);

public sealed record TenantOverviewStoreResponse(Guid Id, string Name, string Timezone);

/// <summary><see cref="Status"/>: <c>"PENDING"</c> | <c>"ACTIVE"</c> | <c>"OFFLINE"</c>. <see cref="Health"/>: mesmo wire format de <c>TenantHealthClassifier</c>.</summary>
public sealed record TenantOverviewInstallationResponse(Guid Id, string Label, string Status, string Health);

/// <summary><see cref="NextAction"/> é a chave (wire, <c>OnboardingStepKeyWireFormat</c>) do próximo passo pendente — nulo quando <see cref="Completed"/> == <see cref="Total"/>.</summary>
public sealed record TenantOverviewDeploymentResponse(int Completed, int Total, string? NextAction);

/// <summary>§15 "[PENDÊNCIA] Definir quais URLs dos aplicativos existem em cada ambiente" — todo campo nulo quando não configurado para o ambiente corrente.</summary>
public sealed record TenantOverviewLinksResponse(string? PublicMenu, string? Admin, string? Health);

public sealed record TenantOverviewResponse(
    TenantOverviewTenantResponse Tenant,
    TenantOverviewOwnerResponse? Owner,
    IReadOnlyList<TenantOverviewStoreResponse> Stores,
    IReadOnlyList<TenantOverviewInstallationResponse> Installations,
    TenantOverviewDeploymentResponse Deployment,
    TenantOverviewLinksResponse Links);
