namespace Nexora.Contracts.Platform;

/// <summary>
/// Contrato do resumo da plataforma (US-150 §7) — <c>GET /v1/platform/summary</c>, consumido pela
/// raiz do <c>web-platform</c> para a "visão geral" exigida pelo shell administrativo. Deliberadamente
/// pequeno (US-150 §15: "o resumo deve permanecer pequeno; análises aprofundadas pertencem às
/// páginas específicas") — só contadores, nunca linha por estabelecimento/instalação.
/// </summary>
public sealed record PlatformTenantsSummaryResponse(int Total, int Active, int Attention);

public sealed record PlatformInstallationsSummaryResponse(int Healthy, int Degraded, int Offline);

public sealed record PlatformSummaryResponse(
    PlatformTenantsSummaryResponse Tenants,
    PlatformInstallationsSummaryResponse Installations,
    int PendingInvites,
    DateTimeOffset GeneratedAt);
