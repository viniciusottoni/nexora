using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Subscriptions;

public class Subscription : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Plan { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public DateTime? TrialStartedAt { get; private set; }
    public DateTime? TrialEndsAt { get; private set; }
    public DateTime? TrialConsumedAt { get; private set; }
    public string? Entitlement { get; private set; }
    public string? RevenueCatCustomerId { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? LastRevenueCatSyncAt { get; private set; }

    private Subscription() { }

    public static Subscription CreateTrial(Guid userId, DateTime trialStartedAt, DateTime trialEndsAt)
    {
        return new Subscription
        {
            UserId = userId,
            Plan = "trial",
            Status = "trial_active",
            TrialStartedAt = trialStartedAt,
            TrialEndsAt = trialEndsAt,
            TrialConsumedAt = trialStartedAt,
        };
    }

    public static Subscription CreateFromPaidPlan(
        Guid userId, string plan, string entitlement,
        string revenueCatCustomerId, DateTime expiresAt, DateTime syncedAt)
    {
        return new Subscription
        {
            UserId = userId,
            Plan = plan,
            Status = expiresAt > syncedAt ? "subscription_active" : "subscription_expired",
            Entitlement = entitlement,
            RevenueCatCustomerId = revenueCatCustomerId,
            ExpiresAt = expiresAt,
            LastRevenueCatSyncAt = syncedAt,
        };
    }

    public bool ExpireTrial()
    {
        if (Status == "trial_expired") return false;
        Status = "trial_expired";
        return true;
    }

    public void ActivatePaidPlan(
        string plan, string entitlement, string revenueCatCustomerId,
        DateTime expiresAt, DateTime syncedAt)
    {
        Plan = plan;
        Entitlement = entitlement;
        RevenueCatCustomerId = revenueCatCustomerId;
        ExpiresAt = expiresAt;
        LastRevenueCatSyncAt = syncedAt;
        Status = expiresAt > syncedAt ? "subscription_active" : "subscription_expired";
        UpdatedAtUtc = syncedAt;
    }

    public bool MarkExpired(DateTime syncedAt)
    {
        if (Status == "subscription_expired") return false;
        Status = "subscription_expired";
        LastRevenueCatSyncAt = syncedAt;
        UpdatedAtUtc = syncedAt;
        return true;
    }


    /// <summary>
    /// US-194: links a RevenueCat customer ID to an existing subscription record
    /// without changing any plan/status/expiry fields.
    /// Used by the SyncEntitlement command so future webhooks can locate the user.
    /// </summary>
    public void LinkRevenueCatCustomerId(string revenueCatCustomerId, DateTime syncedAt)
    {
        RevenueCatCustomerId = revenueCatCustomerId;
        LastRevenueCatSyncAt = syncedAt;
        UpdatedAtUtc = syncedAt;
    }
    /// <summary>
    /// US-194: activates or renews a paid plan from a RevenueCat server-side webhook event.
    /// Unlike <see cref="ActivatePaidPlan"/>, plan is derived from the product catalog on the
    /// server â€” never from untrusted client-supplied data.
    /// </summary>
    public void ActivatePaidPlanFromWebhook(
        string plan,
        string? revenueCatCustomerId,
        DateTime expiresAtUtc,
        DateTime syncedAt)
    {
        Plan = plan;
        RevenueCatCustomerId = revenueCatCustomerId ?? RevenueCatCustomerId;
        ExpiresAt = expiresAtUtc;
        LastRevenueCatSyncAt = syncedAt;
        Status = expiresAtUtc > syncedAt ? "subscription_active" : "subscription_expired";
        UpdatedAtUtc = syncedAt;
    }

    /// <summary>
    /// US-194: creates a subscription record directly from a webhook event (no prior record).
    /// </summary>
    public static Subscription CreateFromWebhook(
        Guid userId,
        string plan,
        string? revenueCatCustomerId,
        DateTime expiresAtUtc,
        DateTime syncedAt)
    {
        return new Subscription
        {
            UserId = userId,
            Plan = plan,
            Status = expiresAtUtc > syncedAt ? "subscription_active" : "subscription_expired",
            RevenueCatCustomerId = revenueCatCustomerId,
            ExpiresAt = expiresAtUtc,
            LastRevenueCatSyncAt = syncedAt,
            CreatedAtUtc = syncedAt,
        };
    }
}
