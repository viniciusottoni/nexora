namespace Awaken.Contracts.Admin.Readiness;

/// <summary>
/// US-218: resposta completa do endpoint de readiness — um card por ambiente.
/// </summary>
public record ReadinessStatusResponse(
    IReadOnlyList<EnvironmentReadinessResponse> Environments,
    DateTime GeneratedAtUtc);
