namespace Awaken.Contracts.Admin.Timeline;

public record ImpactSummaryResponse(
    int EstimatedUsersAffected,
    int ResourcesAffected,
    string? PeakSeverity,
    DateTime? PeriodStart,
    DateTime? PeriodEnd);
