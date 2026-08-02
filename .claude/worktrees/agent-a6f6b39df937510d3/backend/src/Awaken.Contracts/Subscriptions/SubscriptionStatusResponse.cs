namespace Awaken.Contracts.Subscriptions;

public record SubscriptionStatusResponse(
    string AccessStatus,
    DateTime? TrialStartedAt,
    DateTime? TrialEndsAt,
    int? DaysRemaining,
    string? Plan = null,
    DateTime? ExpiresAt = null);
