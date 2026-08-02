using Awaken.Application.Common.Interfaces;
using Awaken.Application.Progression.Common;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Awaken.UnitTests.Progression;

public class WeeklyProgressionReviewerTests
{
    private readonly Mock<IWeeklyProgressionStateRepository> _stateRepository = new();
    private readonly Mock<IQuestLogRepository> _questLogRepository = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<ILogger<WeeklyProgressionReviewer>> _logger = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc); // segunda-feira
    private static readonly UserProfile Profile = UserProfile.Create(
        UserId, goal: "gain_muscle", availableMinutesPerWorkout: 30);

    public WeeklyProgressionReviewerTests()
    {
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
    }

    private WeeklyProgressionReviewer CreateReviewer() => new(
        _stateRepository.Object,
        _questLogRepository.Object,
        _dateTimeService.Object,
        _logger.Object);

    [Fact]
    public async Task ReviewAsync_CreatesInitialStateOnFirstEvaluation()
    {
        _stateRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeeklyProgressionState?)null);
        _questLogRepository.Setup(r => r.GetCompletedSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var plan = await CreateReviewer().ReviewAsync(UserId, Profile, progression: null, CancellationToken.None);

        plan.Decision.Should().Be("hold"); // sem dados de sentimento -> mantém
        plan.MesocycleWeekIndex.Should().Be(1); // primeira avaliação nunca avança o mesociclo
        _stateRepository.Verify(r => r.AddAsync(It.IsAny<WeeklyProgressionState>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviewAsync_ReturnsSamePlanWithoutRedecidingWithinSameWeekAndNoProfileChange()
    {
        var currentWeekAnchor = new DateOnly(2026, 7, 6);
        var existing = WeeklyProgressionState.CreateInitial(UserId, currentWeekAnchor, ComputeHashFor(Profile), UtcNow);
        _stateRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var plan = await CreateReviewer().ReviewAsync(UserId, Profile, progression: null, CancellationToken.None);

        plan.RecalibratedFromProfileChange.Should().BeFalse();
        _questLogRepository.Verify(r => r.GetCompletedSinceAsync(
            It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // CA-004
    public async Task ReviewAsync_RecalibratesWhenProfileHashChanges()
    {
        var currentWeekAnchor = new DateOnly(2026, 7, 6);
        var existing = WeeklyProgressionState.CreateInitial(UserId, currentWeekAnchor, "old-hash", UtcNow);
        _stateRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _questLogRepository.Setup(r => r.GetCompletedSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var plan = await CreateReviewer().ReviewAsync(UserId, Profile, progression: null, CancellationToken.None);

        plan.RecalibratedFromProfileChange.Should().BeTrue();
        _stateRepository.Verify(r => r.Update(existing), Times.Once);
    }

    [Fact]
    public async Task ReviewAsync_AdvancesMesocycleOnNewWeek()
    {
        var previousWeekAnchor = new DateOnly(2026, 6, 29);
        var existing = WeeklyProgressionState.CreateInitial(UserId, previousWeekAnchor, ComputeHashFor(Profile), UtcNow);
        _stateRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _questLogRepository.Setup(r => r.GetCompletedSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var plan = await CreateReviewer().ReviewAsync(UserId, Profile, progression: null, CancellationToken.None);

        plan.MesocycleWeekIndex.Should().Be(2);
        plan.WeekAnchorDate.Should().Be(new DateOnly(2026, 7, 6));
    }

    [Fact]
    public async Task ReviewAsync_UsesRecentPerceivedFeelingsFromQuestLogs()
    {
        var previousWeekAnchor = new DateOnly(2026, 6, 29);
        var existing = WeeklyProgressionState.CreateInitial(UserId, previousWeekAnchor, ComputeHashFor(Profile), UtcNow);
        _stateRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var log = QuestLog.Create(
            Guid.NewGuid(), UserId, "daily", xpEarned: 10,
            strengthXpEarned: 0, agilityXpEarned: 0, enduranceXpEarned: 0, vitalityXpEarned: 0,
            focusXpEarned: 0, wisdomXpEarned: 0, strengthPointsGranted: 0, agilityPointsGranted: 0,
            endurancePointsGranted: 0, vitalityPointsGranted: 0, focusPointsGranted: 0,
            itemsEarned: [], completedAtUtc: UtcNow.AddDays(-1), perceivedFeeling: PerceivedFeelings.TooEasy);
        _questLogRepository.Setup(r => r.GetCompletedSinceAsync(UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([log]);

        var plan = await CreateReviewer().ReviewAsync(UserId, Profile, progression: null, CancellationToken.None);

        plan.Decision.Should().Be("progress");
    }

    private static string ComputeHashFor(UserProfile profile)
    {
        // Recalcula usando um WeeklyProgressionReviewer real (mesma lógica privada),
        // observando o hash via um ciclo completo de ReviewAsync com estado nulo.
        var stateRepository = new Mock<IWeeklyProgressionStateRepository>();
        var questLogRepository = new Mock<IQuestLogRepository>();
        var dateTimeService = new Mock<IDateTimeService>();
        dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
        questLogRepository.Setup(r => r.GetCompletedSinceAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        stateRepository.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeeklyProgressionState?)null);

        WeeklyProgressionState? captured = null;
        stateRepository.Setup(r => r.AddAsync(It.IsAny<WeeklyProgressionState>(), It.IsAny<CancellationToken>()))
            .Callback<WeeklyProgressionState, CancellationToken>((s, _) => captured = s)
            .Returns(Task.CompletedTask);

        var reviewer = new WeeklyProgressionReviewer(
            stateRepository.Object, questLogRepository.Object, dateTimeService.Object,
            new Mock<ILogger<WeeklyProgressionReviewer>>().Object);
        reviewer.ReviewAsync(Guid.NewGuid(), profile, null, CancellationToken.None).GetAwaiter().GetResult();

        return captured!.ProfileSnapshotHash!;
    }
}
