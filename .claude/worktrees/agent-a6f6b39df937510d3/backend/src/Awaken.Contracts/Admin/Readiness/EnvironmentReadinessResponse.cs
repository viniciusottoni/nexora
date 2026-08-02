namespace Awaken.Contracts.Admin.Readiness;

/// <summary>
/// US-218: readiness agregado de um ambiente (dev, staging, produção).
/// </summary>
public record EnvironmentReadinessResponse(
    string Environment,
    string OverallStatus,
    bool HasBlocker,
    IReadOnlyList<ReadinessCheckResponse> Checks,
    DateTime LastCheckedAtUtc);
