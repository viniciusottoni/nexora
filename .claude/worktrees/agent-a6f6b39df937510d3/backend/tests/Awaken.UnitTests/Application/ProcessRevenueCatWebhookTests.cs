using Awaken.Application.Common.Interfaces;
using Awaken.Application.Subscriptions.Commands.ProcessRevenueCatWebhook;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Awaken.UnitTests.Application;

public class ProcessRevenueCatWebhookTests
{
    private readonly Mock<IRevenueCatEventRepository> _rcEventRepo = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepo = new();
    private readonly Mock<IAccessStatusCacheService> _accessStatusCache = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private static readonly DateTime UtcNow = new(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UserId = Guid.NewGuid();

    public ProcessRevenueCatWebhookTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _auditLogService
            .Setup(a => a.RecordAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _accessStatusCache
            .Setup(c => c.InvalidateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private ProcessRevenueCatWebhookCommandHandler CreateHandler() =>
        new(
            _rcEventRepo.Object,
            _subscriptionRepo.Object,
            _accessStatusCache.Object,
            _dateTimeService.Object,
            _unitOfWork.Object,
            _auditLogService.Object,
            NullLogger<ProcessRevenueCatWebhookCommandHandler>.Instance
        );

    // ── Idempotency ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DuplicateEventId_ReturnsSkipped()
    {
        _rcEventRepo
            .Setup(r => r.ExistsByEventIdAsync("evt-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = BuildCommand("evt-001", "INITIAL_PURCHASE", UtcNow.AddDays(30));
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Skipped.Should().BeTrue();
        result.Processed.Should().BeFalse();
        result.Reason.Should().Be("already_processed");

        // No subscription mutation should occur.
        _subscriptionRepo.Verify(r => r.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
        _subscriptionRepo.Verify(r => r.Update(It.IsAny<Subscription>()), Times.Never);
    }

    // ── Activation ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_InitialPurchase_NoExistingSubscription_CreatesAndActivates()
    {
        SetupEventNotExists("evt-002");

        _subscriptionRepo
            .Setup(r => r.GetByRevenueCatCustomerIdAsync(UserId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var expiresAt = UtcNow.AddDays(30);
        var command = BuildCommand("evt-002", "INITIAL_PURCHASE", expiresAt, appUserId: UserId.ToString(), productId: "awaken_monthly");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Processed.Should().BeTrue();
        result.Skipped.Should().BeFalse();

        _subscriptionRepo.Verify(r => r.AddAsync(
            It.Is<Subscription>(s =>
                s.UserId == UserId &&
                s.Status == "subscription_active" &&
                s.Plan == "monthly" &&
                s.ExpiresAt == expiresAt),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_Renewal_ExistingSubscription_ActivatesWithNewExpiry()
    {
        SetupEventNotExists("evt-003");

        var existing = Subscription.CreateFromPaidPlan(UserId, "monthly", "pro", UserId.ToString(), UtcNow.AddDays(-1), UtcNow.AddDays(-30));

        _subscriptionRepo
            .Setup(r => r.GetByRevenueCatCustomerIdAsync(UserId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var newExpiry = UtcNow.AddDays(31);
        var command = BuildCommand("evt-003", "RENEWAL", newExpiry, appUserId: UserId.ToString(), productId: "awaken_monthly");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Processed.Should().BeTrue();
        existing.Status.Should().Be("subscription_active");
        existing.ExpiresAt.Should().Be(newExpiry);

        _subscriptionRepo.Verify(r => r.Update(existing), Times.Once);
    }

    // ── Expiration ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExpirationEvent_DeactivatesSubscription()
    {
        SetupEventNotExists("evt-004");

        var existing = Subscription.CreateFromPaidPlan(UserId, "monthly", "pro", UserId.ToString(), UtcNow.AddDays(10), UtcNow.AddDays(-20));

        _subscriptionRepo
            .Setup(r => r.GetByRevenueCatCustomerIdAsync(UserId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = BuildCommand("evt-004", "EXPIRATION", expiresAt: null, appUserId: UserId.ToString());

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Processed.Should().BeTrue();
        existing.Status.Should().Be("subscription_expired");

        _subscriptionRepo.Verify(r => r.Update(existing), Times.Once);
    }

    [Fact]
    public async Task Handle_CancellationEvent_DeactivatesSubscription()
    {
        SetupEventNotExists("evt-005");

        var existing = Subscription.CreateFromPaidPlan(UserId, "annual", "pro", UserId.ToString(), UtcNow.AddDays(200), UtcNow.AddDays(-100));

        _subscriptionRepo
            .Setup(r => r.GetByRevenueCatCustomerIdAsync(UserId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = BuildCommand("evt-005", "CANCELLATION", expiresAt: null, appUserId: UserId.ToString());

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Processed.Should().BeTrue();
        existing.Status.Should().Be("subscription_expired");
    }

    // ── Cache invalidation ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidActivation_InvalidatesAccessStatusCache()
    {
        SetupEventNotExists("evt-006");

        _subscriptionRepo
            .Setup(r => r.GetByRevenueCatCustomerIdAsync(UserId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var command = BuildCommand("evt-006", "INITIAL_PURCHASE", UtcNow.AddDays(30), appUserId: UserId.ToString(), productId: "awaken_annual");

        await CreateHandler().Handle(command, CancellationToken.None);

        _accessStatusCache.Verify(c => c.InvalidateAsync(UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetupEventNotExists(string eventId)
    {
        _rcEventRepo
            .Setup(r => r.ExistsByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _rcEventRepo
            .Setup(r => r.AddAsync(It.IsAny<RevenueCatEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static ProcessRevenueCatWebhookCommand BuildCommand(
        string eventId,
        string eventType,
        DateTime? expiresAt,
        string? appUserId = null,
        string? productId = null) =>
        new(
            EventId: eventId,
            AppUserId: appUserId ?? UserId.ToString(),
            EventType: eventType,
            ProductId: productId,
            OriginalTransactionId: "orig-txn-001",
            ExpiresAtUtc: expiresAt,
            PayloadHash: "AABBCCDD11223344");
}
