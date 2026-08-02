namespace Awaken.Contracts.Admin.Analytics;

/// <summary>
/// US-169 — engajamento e retenção por coorte.
/// RN-003: retenção indica "dados insuficientes" em vez de uma taxa zero enganosa
/// quando a coorte ainda não tem tempo suficiente decorrido.
/// </summary>
public record EngagementMetricsResponse(
    int Dau,
    int Mau,
    double? DauMauRatio,
    RetentionCohort? RetentionD1,
    RetentionCohort? RetentionD7,
    RetentionCohort? RetentionD30,
    IReadOnlyList<FeatureUsageItem> FeatureUsage);

public record RetentionCohort(double? RetentionRate, bool InsufficientData);

public record FeatureUsageItem(string Feature, int UsageCount);
