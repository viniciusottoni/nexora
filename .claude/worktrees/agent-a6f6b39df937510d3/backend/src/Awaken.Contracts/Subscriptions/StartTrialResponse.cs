namespace Awaken.Contracts.Subscriptions;

public record StartTrialResponse(
    string AccessStatus,
    DateTime TrialStartedAt,
    DateTime TrialEndsAt);
