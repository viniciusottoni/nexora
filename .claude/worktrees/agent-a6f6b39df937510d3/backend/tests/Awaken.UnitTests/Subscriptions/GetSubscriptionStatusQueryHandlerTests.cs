using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Subscriptions.Queries.GetSubscriptionStatus;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Subscriptions;

public class GetSubscriptionStatusQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<GetSubscriptionStatusQueryHandler>> _logger = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 6, 18, 10, 0, 0, DateTimeKind.Utc);

    public GetSubscriptionStatusQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(_userId);
        _dateTimeService.Setup(d => d.UtcNow).Returns(_utcNow);
    }

    private GetSubscriptionStatusQueryHandler CreateHandler() => new(
        _userRepository.Object,
        _subscriptionRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _logger.Object);

    [Fact]
    public async Task HandleReturnsNoTrialWhenUserHasNoSubscription()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(), CancellationToken.None);

        result.AccessStatus.Should().Be("no_trial");
        result.TrialStartedAt.Should().BeNull();
        result.TrialEndsAt.Should().BeNull();
        result.DaysRemaining.Should().BeNull();
    }

    [Fact]
    public async Task HandleReturnsTrialActiveWithDaysRemainingWhenTrialIsOngoing()
    {
        var trialStartedAt = _utcNow;
        var trialEndsAt = _utcNow.AddDays(7);

        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(trialEndsAt);

        var subscription = Subscription.CreateTrial(_userId, trialStartedAt, trialEndsAt);

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(), CancellationToken.None);

        result.AccessStatus.Should().Be("trial_active");
        result.TrialStartedAt.Should().Be(trialStartedAt);
        result.TrialEndsAt.Should().Be(trialEndsAt);
        result.DaysRemaining.Should().Be(7);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleReturnsTrialExpiredAndExpiresSubscriptionWhenTrialEnded()
    {
        var trialStartedAt = _utcNow.AddDays(-8);
        var trialEndsAt = _utcNow.AddDays(-1);

        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(trialEndsAt);

        var subscription = Subscription.CreateTrial(_userId, trialStartedAt, trialEndsAt);

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(), CancellationToken.None);

        result.AccessStatus.Should().Be("trial_expired");
        result.DaysRemaining.Should().BeNull();
        subscription.Status.Should().Be("trial_expired");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleIsIdempotentWhenTrialAlreadyExpired()
    {
        var trialStartedAt = _utcNow.AddDays(-8);
        var trialEndsAt = _utcNow.AddDays(-1);

        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(trialEndsAt);

        var subscription = Subscription.CreateTrial(_userId, trialStartedAt, trialEndsAt);
        subscription.ExpireTrial();

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(), CancellationToken.None);

        result.AccessStatus.Should().Be("trial_expired");
        subscription.Status.Should().Be("trial_expired");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleThrowsUnauthorizedWhenUserNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(new GetSubscriptionStatusQuery(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("SESSION_INVALID");
    }

    [Fact]
    public async Task HandleReturnsDaysRemainingCeiledWhenPartialDayLeft()
    {
        var utcNow = new DateTime(2026, 6, 18, 10, 30, 0, DateTimeKind.Utc);
        _dateTimeService.Setup(d => d.UtcNow).Returns(utcNow);

        var trialEndsAt = new DateTime(2026, 6, 19, 10, 0, 0, DateTimeKind.Utc);

        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(trialEndsAt);

        var subscription = Subscription.CreateTrial(_userId, utcNow.AddDays(-6), trialEndsAt);

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(), CancellationToken.None);

        result.DaysRemaining.Should().Be(1);
    }

    [Fact]
    public async Task HandleReturnsThreeDaysRemainingWhenTrialIsOnTheFinalStretch()
    {
        var trialStartedAt = _utcNow.AddDays(-4);
        var trialEndsAt = _utcNow.AddDays(3);

        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(trialEndsAt);

        var subscription = Subscription.CreateTrial(_userId, trialStartedAt, trialEndsAt);

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(), CancellationToken.None);

        result.AccessStatus.Should().Be("trial_active");
        result.DaysRemaining.Should().Be(3);
    }

    [Fact]
    public async Task HandleReturnsSubscriptionActiveWhenPaidPlanNotExpired()
    {
        var expiresAt = _utcNow.AddDays(30);
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var subscription = Subscription.CreateFromPaidPlan(_userId, "monthly", "pro_access", "rc_123", expiresAt, _utcNow);

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(), CancellationToken.None);

        result.AccessStatus.Should().Be("subscription_active");
        result.Plan.Should().Be("monthly");
        result.ExpiresAt.Should().Be(expiresAt);
        result.TrialStartedAt.Should().BeNull();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleReturnsSubscriptionExpiredAndMarksItWhenPaidPlanExpired()
    {
        var expiresAt = _utcNow.AddDays(-5);
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var subscription = Subscription.CreateFromPaidPlan(_userId, "annual", "pro_access", "rc_123", expiresAt, _utcNow.AddDays(-6));

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(), CancellationToken.None);

        result.AccessStatus.Should().Be("subscription_expired");
        result.Plan.Should().Be("annual");
        subscription.Status.Should().Be("subscription_expired");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandlePaidPlanTakesPriorityOverExpiredTrial()
    {
        var expiresAt = _utcNow.AddDays(15);
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(_utcNow.AddDays(-1));
        var subscription = Subscription.CreateTrial(_userId, _utcNow.AddDays(-8), _utcNow.AddDays(-1));
        subscription.ActivatePaidPlan("monthly", "pro_access", "rc_123", expiresAt, _utcNow);

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(), CancellationToken.None);

        result.AccessStatus.Should().Be("subscription_active");
        result.Plan.Should().Be("monthly");
    }

    [Fact]
    public async Task HandleLogsTrialExpiredWhenTrialJustExpires()
    {
        var trialStartedAt = _utcNow.AddDays(-8);
        var trialEndsAt = _utcNow.AddDays(-1);

        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(trialEndsAt);

        var subscription = Subscription.CreateTrial(_userId, trialStartedAt, trialEndsAt);

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        await CreateHandler().Handle(new GetSubscriptionStatusQuery(), CancellationToken.None);

        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("trial_expired")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleDoesNotLogTrialExpiredWhenAlreadyExpired()
    {
        var trialStartedAt = _utcNow.AddDays(-8);
        var trialEndsAt = _utcNow.AddDays(-1);

        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(trialEndsAt);

        var subscription = Subscription.CreateTrial(_userId, trialStartedAt, trialEndsAt);
        subscription.ExpireTrial(); // Already expired

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        await CreateHandler().Handle(new GetSubscriptionStatusQuery(), CancellationToken.None);

        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("trial_expired")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
