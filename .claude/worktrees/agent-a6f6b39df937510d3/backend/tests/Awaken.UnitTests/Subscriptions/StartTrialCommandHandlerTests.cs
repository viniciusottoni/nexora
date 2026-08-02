using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Subscriptions.Commands.StartTrial;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Subscriptions;

public class StartTrialCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<StartTrialCommandHandler>> _logger = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IAccessStatusCacheService> _accessStatusCache = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 6, 18, 10, 0, 0, DateTimeKind.Utc);

    public StartTrialCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(_userId);
        _dateTimeService.Setup(d => d.UtcNow).Returns(_utcNow);
    }

    private StartTrialCommandHandler CreateHandler() => new(
        _userRepository.Object,
        _subscriptionRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _logger.Object,
        _auditLogService.Object,
        _accessStatusCache.Object);

    [Fact]
    public async Task HandleReturnsTrialActiveWhenUserIsEligible()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        _subscriptionRepository.Setup(r => r.HasAnySubscriptionRecordAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateHandler().Handle(new StartTrialCommand(), CancellationToken.None);

        result.AccessStatus.Should().Be("trial_active");
        result.TrialStartedAt.Should().Be(_utcNow);
        result.TrialEndsAt.Should().Be(_utcNow.AddDays(7));
        _subscriptionRepository.Verify(r => r.AddAsync(
            It.Is<Subscription>(s =>
                s.UserId == _userId &&
                s.Plan == "trial" &&
                s.Status == "trial_active" &&
                s.TrialStartedAt == _utcNow &&
                s.TrialEndsAt == _utcNow.AddDays(7) &&
                s.TrialConsumedAt == _utcNow),
            It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        user.TrialEndsAt.Should().Be(_utcNow.AddDays(7));
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("trial_started") &&
                    v.ToString()!.Contains($"userId={_userId}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleThrowsConflictWhenUserAlreadyUsedTrial()
    {
        _subscriptionRepository.Setup(r => r.HasAnySubscriptionRecordAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => CreateHandler().Handle(new StartTrialCommand(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("TRIAL_ALREADY_USED");
        _subscriptionRepository.Verify(r => r.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleThrowsConflictWhenUserHasPreviousPaidSubscriptionWithoutEverTrialing()
    {
        _subscriptionRepository.Setup(r => r.HasAnySubscriptionRecordAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => CreateHandler().Handle(new StartTrialCommand(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("TRIAL_ALREADY_USED");
        _userRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleThrowsUnauthorizedWhenUserNotFound()
    {
        _subscriptionRepository.Setup(r => r.HasAnySubscriptionRecordAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(new StartTrialCommand(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("SESSION_INVALID");
    }
}
