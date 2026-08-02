using Awaken.Domain.Entities.Subscriptions;
using FluentAssertions;

namespace Awaken.UnitTests.Domain;

public class SubscriptionTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 6, 18, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateFromPaidPlan_SetsSubscriptionActiveWhenExpiresInFuture()
    {
        var expiresAt = _utcNow.AddDays(30);

        var subscription = Subscription.CreateFromPaidPlan(
            _userId, "monthly", "pro_access", "rc_customer_123", expiresAt, _utcNow);

        subscription.UserId.Should().Be(_userId);
        subscription.Plan.Should().Be("monthly");
        subscription.Status.Should().Be("subscription_active");
        subscription.Entitlement.Should().Be("pro_access");
        subscription.RevenueCatCustomerId.Should().Be("rc_customer_123");
        subscription.ExpiresAt.Should().Be(expiresAt);
        subscription.LastRevenueCatSyncAt.Should().Be(_utcNow);
    }

    [Fact]
    public void CreateFromPaidPlan_SetsSubscriptionExpiredWhenExpiresInPast()
    {
        var expiresAt = _utcNow.AddDays(-1);

        var subscription = Subscription.CreateFromPaidPlan(
            _userId, "annual", "pro_access", "rc_customer_123", expiresAt, _utcNow);

        subscription.Status.Should().Be("subscription_expired");
    }

    [Fact]
    public void CreateTrial_SetsTrialConsumedAtToTrialStartedAt()
    {
        var trialStartedAt = _utcNow;
        var trialEndsAt = _utcNow.AddDays(7);

        var subscription = Subscription.CreateTrial(_userId, trialStartedAt, trialEndsAt);

        subscription.TrialConsumedAt.Should().Be(trialStartedAt);
    }

    [Fact]
    public void ActivatePaidPlan_UpdatesAllFieldsAndSetsActive()
    {
        var trialSubscription = Subscription.CreateTrial(_userId, _utcNow.AddDays(-8), _utcNow.AddDays(-1));
        var expiresAt = _utcNow.AddDays(30);

        trialSubscription.ActivatePaidPlan("monthly", "pro_access", "rc_123", expiresAt, _utcNow);

        trialSubscription.Plan.Should().Be("monthly");
        trialSubscription.Status.Should().Be("subscription_active");
        trialSubscription.Entitlement.Should().Be("pro_access");
        trialSubscription.RevenueCatCustomerId.Should().Be("rc_123");
        trialSubscription.ExpiresAt.Should().Be(expiresAt);
        trialSubscription.LastRevenueCatSyncAt.Should().Be(_utcNow);
    }

    [Fact]
    public void ActivatePaidPlan_SetsExpiredWhenExpiresInPast()
    {
        var subscription = Subscription.CreateTrial(_userId, _utcNow.AddDays(-8), _utcNow.AddDays(-1));
        var expiresAt = _utcNow.AddDays(-2);

        subscription.ActivatePaidPlan("monthly", "pro_access", "rc_123", expiresAt, _utcNow);

        subscription.Status.Should().Be("subscription_expired");
    }

    [Fact]
    public void MarkExpired_ReturnsTrueAndSetsStatusWhenNotAlreadyExpired()
    {
        var subscription = Subscription.CreateFromPaidPlan(
            _userId, "monthly", "pro_access", "rc_123", _utcNow.AddDays(1), _utcNow);

        var result = subscription.MarkExpired(_utcNow);

        result.Should().BeTrue();
        subscription.Status.Should().Be("subscription_expired");
        subscription.LastRevenueCatSyncAt.Should().Be(_utcNow);
    }

    [Fact]
    public void MarkExpired_ReturnsFalseWhenAlreadyExpired()
    {
        var subscription = Subscription.CreateFromPaidPlan(
            _userId, "monthly", "pro_access", "rc_123", _utcNow.AddDays(-1), _utcNow);

        var result = subscription.MarkExpired(_utcNow);

        result.Should().BeFalse();
        subscription.Status.Should().Be("subscription_expired");
    }
}
