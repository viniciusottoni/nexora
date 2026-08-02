using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Progression.Services;
using Awaken.Application.Quests.Queries.GetDailyQuest;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Quests;

public class GetDailyQuestQueryHandlerTests
{
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IUserDateService> _userDateService = new();
    private readonly Mock<IDailyQuestPenaltyService> _dailyQuestPenaltyService = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 6, 22);

    public GetDailyQuestQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _userDateService.Setup(s => s.TodayLocal).Returns(Today);
        _dailyQuestPenaltyService
            .Setup(s => s.ApplyForUserBeforeDateAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DailyPenaltyRolloverSummary(0, 0));
    }

    private GetDailyQuestQueryHandler CreateHandler() => new(
        _questRepository.Object,
        _currentUserService.Object,
        _userDateService.Object,
        _dailyQuestPenaltyService.Object);

    [Fact]
    public async Task US047_ReturnsPersistedQuest_WhenItExistsForToday()
    {
        var questDateUtc = Today.ToDateTime(TimeOnly.MinValue);
        var quest = Quest.Create(UserId, questDateUtc, "pt-BR", "key");
        quest.AssignWorkout("""{ "title": "Daily Quest", "durationMinutes": 30, "exercises": [] }""", DateTime.UtcNow);
        _questRepository
            .Setup(r => r.GetByUserIdAndDateAsync(UserId, "daily", questDateUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new GetDailyQuestQuery(), CancellationToken.None);

        result.Id.Should().Be(quest.Id);
        result.Workout!.DurationMinutes.Should().Be(30);
    }

    [Fact]
    public async Task UsesLocalDate_AndAppliesPendingPenaltiesBeforeReturningDaily()
    {
        var localToday = new DateOnly(2026, 6, 23);
        var questDateUtc = DateTime.SpecifyKind(localToday.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var quest = Quest.Create(UserId, questDateUtc, "pt-BR", "key");
        quest.AssignWorkout("""{ "title": "Daily Quest", "durationMinutes": 25, "exercises": [] }""", DateTime.UtcNow);

        _userDateService.Setup(s => s.TodayLocal).Returns(localToday);
        _questRepository
            .Setup(r => r.GetByUserIdAndDateAsync(UserId, "daily", questDateUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new GetDailyQuestQuery(), CancellationToken.None);

        result.Id.Should().Be(quest.Id);
        _dailyQuestPenaltyService.Verify(
            s => s.ApplyForUserBeforeDateAsync(UserId, questDateUtc, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ThrowsNotFound_WhenNoQuestExistsForToday()
    {
        _questRepository
            .Setup(r => r.GetByUserIdAndDateAsync(UserId, "daily", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Quest?)null);

        var act = () => CreateHandler().Handle(new GetDailyQuestQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
