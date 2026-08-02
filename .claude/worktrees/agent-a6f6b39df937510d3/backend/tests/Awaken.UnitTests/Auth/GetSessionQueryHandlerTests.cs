using Awaken.Application.Auth.Queries.GetSession;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Auth;

public class GetSessionQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();

    private readonly DateTime _utcNow = new(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc);

    public GetSessionQueryHandlerTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(_utcNow);
    }

    private GetSessionQueryHandler CreateHandler() => new(
        _userRepository.Object,
        _subscriptionRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object);

    [Fact]
    public async Task HandleReturnsSessionResponseWithNoTrialForNewUser()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hash", "Hunter", "pt-BR");

        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var result = await CreateHandler().Handle(new GetSessionQuery(), CancellationToken.None);

        result.IsAuthenticated.Should().BeTrue();
        result.AccessStatus.Should().Be("no_trial");
        result.OnboardingCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task HandleReturnsSubscriptionActiveWhenPaidPlanExistsAfterTrialExpired()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hash", "Hunter", "pt-BR");
        user.StartTrial(_utcNow.AddDays(-1));
        var subscription = Subscription.CreateFromPaidPlan(
            user.Id,
            "monthly",
            "pro_access",
            "rc_123",
            _utcNow.AddDays(30),
            _utcNow);

        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await CreateHandler().Handle(new GetSessionQuery(), CancellationToken.None);

        result.AccessStatus.Should().Be("subscription_active");
    }

    [Fact]
    public async Task HandleThrowsWhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(new GetSessionQuery(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("SESSION_INVALID");
    }
}
