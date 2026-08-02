// US-194 + US-205: SyncEntitlementCommand is read-only (returns server-authoritative status);
// cache is invalidated on every sync to ensure the next middleware check is fresh.
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Subscriptions.Commands.SyncEntitlement;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Subscriptions;

public class SyncEntitlementCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IAccessStatusCacheService> _accessStatusCache = new();
    private readonly Mock<IRevenueCatValidationService> _revenueCatValidationService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<SyncEntitlementCommandHandler>> _logger = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IConfiguration> _configuration = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 6, 18, 10, 0, 0, DateTimeKind.Utc);

    public SyncEntitlementCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(_userId);
        _dateTimeService.Setup(d => d.UtcNow).Returns(_utcNow);
        _revenueCatValidationService
            .Setup(s => s.ValidateSubscriptionAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RevenueCatSubscriptionValidation(false, null, null, null, null));
    }

    private SyncEntitlementCommandHandler CreateHandler() => new(
        _userRepository.Object,
        _subscriptionRepository.Object,
        _currentUserService.Object,
        _accessStatusCache.Object,
        _revenueCatValidationService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _logger.Object,
        _auditLogService.Object,
        _configuration.Object);

    private static SyncEntitlementCommand BuildCommand(string rcCustomerId = "rc_customer_123") =>
        new(rcCustomerId);

    [Fact]
    public async Task HandleReturnsNoSubscriptionWhenNoneExists()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var result = await CreateHandler().Handle(BuildCommand(), CancellationToken.None);

        result.AccessStatus.Should().Be("no_subscription");
        result.Plan.Should().BeNull();
        result.ExpiresAt.Should().BeNull();
        result.AccessRestored.Should().BeFalse();
        // No DB write when no subscription record needs updating.
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleActivatesSubscriptionFromRevenueCatLookupWhenActive()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var expiresAt = _utcNow.AddDays(30);

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _revenueCatValidationService
            .Setup(s => s.ValidateSubscriptionAsync("rc_customer_123", null, _utcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RevenueCatSubscriptionValidation(
                true,
                "monthly",
                "Awaken Unlimited",
                "awaken_monthly",
                expiresAt));

        var result = await CreateHandler().Handle(BuildCommand(), CancellationToken.None);

        result.AccessStatus.Should().Be("subscription_active");
        result.Plan.Should().Be("monthly");
        result.ExpiresAt.Should().Be(expiresAt);
        _subscriptionRepository.Verify(r => r.AddAsync(
            It.Is<Subscription>(s =>
                s.UserId == _userId &&
                s.Plan == "monthly" &&
                s.Status == "subscription_active" &&
                s.RevenueCatCustomerId == "rc_customer_123" &&
                s.ExpiresAt == expiresAt),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleRestoresExpiredTrialFromRevenueCatLookup()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var existing = Subscription.CreateTrial(_userId, _utcNow.AddDays(-10), _utcNow.AddDays(-3));
        existing.ExpireTrial();
        var expiresAt = _utcNow.AddDays(365);

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _revenueCatValidationService
            .Setup(s => s.ValidateSubscriptionAsync("rc_customer_123", null, _utcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RevenueCatSubscriptionValidation(
                true,
                "annual",
                "Awaken Unlimited",
                "awaken_yearly",
                expiresAt));

        var result = await CreateHandler().Handle(BuildCommand(), CancellationToken.None);

        result.AccessStatus.Should().Be("subscription_active");
        result.Plan.Should().Be("annual");
        result.AccessRestored.Should().BeTrue();
        existing.RevenueCatCustomerId.Should().Be("rc_customer_123");
        existing.ExpiresAt.Should().Be(expiresAt);
        _subscriptionRepository.Verify(r => r.Update(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleReturnsSubscriptionActiveWhenPaidPlanNotExpired()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var expiresAt = _utcNow.AddDays(30);
        var existing = Subscription.CreateFromPaidPlan(
            _userId, "monthly", "pro_access", "rc_customer_123", expiresAt, _utcNow.AddMinutes(-5));

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateHandler().Handle(BuildCommand(), CancellationToken.None);

        result.AccessStatus.Should().Be("subscription_active");
        result.Plan.Should().Be("monthly");
        result.ExpiresAt.Should().Be(expiresAt);
        result.AccessRestored.Should().BeFalse();
    }

    [Fact]
    public async Task HandleReturnsSubscriptionExpiredWhenPaidPlanExpired()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var expiresAt = _utcNow.AddDays(-5);
        var existing = Subscription.CreateFromPaidPlan(
            _userId, "monthly", "pro_access", "rc_customer_123", expiresAt, _utcNow.AddDays(-5));

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateHandler().Handle(BuildCommand(), CancellationToken.None);

        result.AccessStatus.Should().Be("subscription_expired");
    }

    [Fact]
    public async Task HandleReturnsTrialActiveWhenTrialNotExpired()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var trialEndsAt = _utcNow.AddDays(5);
        var existing = Subscription.CreateTrial(_userId, _utcNow.AddDays(-2), trialEndsAt);

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateHandler().Handle(BuildCommand(), CancellationToken.None);

        result.AccessStatus.Should().Be("trial_active");
    }

    [Fact]
    public async Task HandleLinksRevenueCatCustomerIdWhenNotSet()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        // Trial subscription with no RevenueCatCustomerId yet.
        var existing = Subscription.CreateTrial(_userId, _utcNow.AddDays(-2), _utcNow.AddDays(5));
        existing.RevenueCatCustomerId.Should().BeNull();

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await CreateHandler().Handle(BuildCommand("rc_new_id"), CancellationToken.None);

        existing.RevenueCatCustomerId.Should().Be("rc_new_id");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleUsesFallbackRevenueCatCustomerIdWhenPrimaryIsMissing()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var expiresAt = _utcNow.AddDays(30);
        var existing = Subscription.CreateTrial(_userId, _utcNow.AddDays(-2), _utcNow.AddDays(5));
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _revenueCatValidationService
            .Setup(s => s.ValidateSubscriptionAsync(
                "rc_primary_missing",
                "rc_fallback_found",
                _utcNow,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RevenueCatSubscriptionValidation(
                true,
                "monthly",
                "Awaken Unlimited",
                "monthly",
                expiresAt));

        var command = new SyncEntitlementCommand(
            "rc_primary_missing",
            "rc_fallback_found");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AccessStatus.Should().Be("subscription_active");
        result.Plan.Should().Be("monthly");
        result.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public async Task HandleDoesNotSaveWhenRevenueCatCustomerIdAlreadySet()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var existing = Subscription.CreateFromPaidPlan(
            _userId, "monthly", "pro_access", "rc_existing_id", _utcNow.AddDays(30), _utcNow.AddDays(-1));

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await CreateHandler().Handle(BuildCommand("rc_existing_id"), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleInvalidatesCacheOnEverySync()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        await CreateHandler().Handle(BuildCommand(), CancellationToken.None);

        _accessStatusCache.Verify(c => c.InvalidateAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleThrowsUnauthorizedWhenUserNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(BuildCommand(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("SESSION_INVALID");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
