using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Queries.GetQuestRewardSummary;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Quests;

public class GetQuestRewardSummaryQueryHandlerTests
{
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<IQuestLogRepository> _questLogRepository = new();
    private readonly Mock<IHunterProgressionRepository> _hunterProgressionRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 24, 9, 0, 0, DateTimeKind.Utc);

    public GetQuestRewardSummaryQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
    }

    private GetQuestRewardSummaryQueryHandler CreateHandler() => new(
        _questRepository.Object,
        _questLogRepository.Object,
        _hunterProgressionRepository.Object,
        _currentUserService.Object);

    private static Quest BuildCompletedDungeon(Guid userId)
    {
        var quest = Quest.Create(userId, new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");
        typeof(Quest).GetProperty(nameof(Quest.Type))!.SetValue(quest, "dungeon");
        quest.Start(UtcNow, []);
        quest.Complete(220, DateTime.UtcNow);
        return quest;
    }

    [Fact]
    public async Task CA001_ReturnsRewardSummary_FromQuestLog()
    {
        var quest = BuildCompletedDungeon(UserId);
        var log = QuestLog.Create(
            quest.Id, UserId, "dungeon", 220,
            strengthXpEarned: 9, agilityXpEarned: 0, enduranceXpEarned: 0, vitalityXpEarned: 5, focusXpEarned: 0, wisdomXpEarned: 7,
            strengthPointsGranted: 1, agilityPointsGranted: 0, endurancePointsGranted: 0, vitalityPointsGranted: 0, focusPointsGranted: 0,
            itemsEarned: ["pedra_dungeon"], completedAtUtc: UtcNow);
        var progression = HunterProgression.Create(UserId);
        progression.AddAttributeXp(10, 0, 0, 0, 0, 0, 1.0m, UtcNow);
        progression.UpdateStreakAfterQuestCompletion(UtcNow);

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);
        _questLogRepository.Setup(r => r.GetByQuestIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(log);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(progression);

        var result = await CreateHandler().Handle(new GetQuestRewardSummaryQuery(quest.Id), CancellationToken.None);

        result.QuestType.Should().Be("dungeon");
        result.XpEarned.Should().Be(220);
        result.AttributeXpEarned.Strength.Should().Be(9);
        result.AttributeXpEarned.Wisdom.Should().Be(7);
        result.AttributePointsGranted.Strength.Should().Be(1);
        result.StreakDays.Should().Be(1);
        result.ItemsEarned.Should().ContainSingle().Which.Should().Be("pedra_dungeon");
        result.AttributeLevelUps.Should().ContainSingle().Which.Should().Be("strength");
        result.AttributeLevelUpDetails.Should().ContainSingle(levelUp =>
            levelUp.Attribute == "strength" &&
            levelUp.NewLevel == 2 &&
            levelUp.Source == "dungeon");
        result.ItemRewards.Should().ContainSingle(reward =>
            reward.ItemId == "pedra_dungeon" &&
            reward.Rarity == "consumable" &&
            reward.Source == "dungeon");
    }

    [Fact]
    public async Task CA002_DailyWithoutItems_ReturnsEmptyItemsList()
    {
        var quest = Quest.Create(UserId, new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");
        quest.Start(UtcNow, []);
        quest.Complete(24, DateTime.UtcNow);
        var log = QuestLog.Create(
            quest.Id, UserId, "daily", 24,
            strengthXpEarned: 4, agilityXpEarned: 0, enduranceXpEarned: 0, vitalityXpEarned: 0, focusXpEarned: 0, wisdomXpEarned: 1,
            strengthPointsGranted: 4, agilityPointsGranted: 0, endurancePointsGranted: 0, vitalityPointsGranted: 0, focusPointsGranted: 0,
            itemsEarned: [], completedAtUtc: UtcNow);

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);
        _questLogRepository.Setup(r => r.GetByQuestIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(log);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new GetQuestRewardSummaryQuery(quest.Id), CancellationToken.None);

        result.ItemsEarned.Should().BeEmpty();
        result.StreakDays.Should().Be(0);
    }

    [Fact]
    public async Task ThrowsConflict_WhenQuestHasNoLogYet()
    {
        var quest = Quest.Create(UserId, new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);
        _questLogRepository.Setup(r => r.GetByQuestIdAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync((QuestLog?)null);

        var act = () => CreateHandler().Handle(new GetQuestRewardSummaryQuery(quest.Id), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Code.Should().Be("QUEST_NOT_COMPLETED");
    }

    [Fact]
    public async Task ThrowsNotFound_WhenQuestDoesNotExist()
    {
        var questId = Guid.NewGuid();
        _questRepository.Setup(r => r.GetByIdAsync(questId, It.IsAny<CancellationToken>())).ReturnsAsync((Quest?)null);

        var act = () => CreateHandler().Handle(new GetQuestRewardSummaryQuery(questId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ThrowsNotFound_WhenQuestBelongsToAnotherUser()
    {
        var quest = Quest.Create(Guid.NewGuid(), new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(new GetQuestRewardSummaryQuery(quest.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
