namespace Awaken.Contracts.AppConfig;

public record PlansConfigResponse(int TrialDays, IReadOnlyList<PlanConfigResponse> Plans);

public record PlanConfigResponse(
    string Id,
    string Label,
    string PriceLabel,
    string PeriodLabel,
    string? SavingLabel,
    bool Highlighted);
