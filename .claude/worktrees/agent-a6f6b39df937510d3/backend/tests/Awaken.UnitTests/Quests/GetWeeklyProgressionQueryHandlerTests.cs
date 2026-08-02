using Awaken.Application.Common.Interfaces;
using Awaken.Application.Progression.Common;
using Awaken.Application.Quests.Queries.GetWeeklyProgression;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Awaken.UnitTests.Quests;

public class GetWeeklyProgressionQueryHandlerTests
{
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly Mock<IHunterProgressionRepository> _hunterProgressionRepository = new();
    private readonly Mock<IWeeklyProgressionStateRepository> _stateRepository = new();
    private readonly Mock<IQuestLogRepository> _questLogRepository = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ILogger<WeeklyProgressionReviewer>> _reviewerLogger = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc);
    private static readonly UserProfile Profile = UserProfile.Create(UserId, goal: "gain_muscle");

    public GetWeeklyProgressionQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
    }

    private GetWeeklyProgressionQueryHandler CreateHandler()
    {
        var reviewer = new WeeklyProgressionReviewer(
            _stateRepository.Object, _questLogRepository.Object, _dateTimeService.Object, _reviewerLogger.Object);

        return new GetWeeklyProgressionQueryHandler(
            _userProfileRepository.Object, _hunterProgressionRepository.Object, reviewer, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_ReturnsWeeklyProgressionPlanForCurrentUser()
    {
        _userProfileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);
        _stateRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeeklyProgressionState?)null);
        _questLogRepository.Setup(r => r.GetCompletedSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var response = await CreateHandler().Handle(new GetWeeklyProgressionQuery(), CancellationToken.None);

        response.Rank.Should().Be("E");
        response.Decision.Should().NotBeNullOrEmpty();
        response.WeekAnchorDate.Should().Be("2026-07-06");
    }

    [Fact]
    public async Task Handle_ReturnsRankFromHunterProgressionWhenAvailable()
    {
        var progression = HunterProgression.CreateFromOnboarding(UserId, 8, 8, 8, 8, 8, 1);
        _userProfileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        _stateRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeeklyProgressionState?)null);
        _questLogRepository.Setup(r => r.GetCompletedSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var response = await CreateHandler().Handle(new GetWeeklyProgressionQuery(), CancellationToken.None);

        response.Rank.Should().Be(progression.Rank);
    }
}
