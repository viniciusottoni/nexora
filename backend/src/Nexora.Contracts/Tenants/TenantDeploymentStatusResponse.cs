namespace Nexora.Contracts.Tenants;

/// <summary>
/// US-156 · Recuperação do provisionamento e token de instalação — porta de
/// <c>GET /v1/platform/tenants/{tenantId}/deployment</c>. PARECIDO com <c>TenantOverviewDeploymentResponse</c>
/// (US-152, <c>GetTenantOverviewQueryHandler.BuildDeploymentAsync</c>) — mesma contagem de passos do
/// roteiro de implantação (<c>OnboardingStep</c>) — mas ENRIQUECIDO com <see cref="Installation"/>
/// (status + se admite reemissão de token agora), que o overview não expõe. Deliberadamente uma
/// query/handler/contrato PRÓPRIOS (<c>GetTenantDeploymentStatusQuery</c>) em vez de estender o
/// endpoint de overview existente — ver decisão registrada no relatório desta tarefa.
/// </summary>
public sealed record TenantDeploymentStatusResponse(
    int Completed,
    int Total,
    TenantDeploymentInstallationResponse? Installation,
    string? NextAction);

/// <summary>
/// <see cref="Status"/>: mesmo vocabulário de <c>TenantOverviewInstallationResponse</c>
/// (<c>"PENDING"</c> | <c>"ACTIVE"</c> | <c>"OFFLINE"</c>). <see cref="CanReissueToken"/> espelha
/// <see cref="Nexora.Domain.Platform.EdgeInstallation.CanReissueToken"/> — verdadeiro enquanto a
/// instalação ainda não concluiu o pareamento.
/// </summary>
public sealed record TenantDeploymentInstallationResponse(Guid Id, string Status, bool CanReissueToken);
