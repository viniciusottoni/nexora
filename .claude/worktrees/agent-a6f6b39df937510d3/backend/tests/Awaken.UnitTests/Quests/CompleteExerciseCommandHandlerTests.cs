using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Commands.CompleteExercise;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Quests;

public class CompleteExerciseCommandHandlerTests
{
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<IHunterProgressionRepository> _hunterProgressionRepository = new();
    private readonly Mock<IRankScoreLogRepository> _rankScoreLogRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<CompleteExerciseCommandHandler>> _logger = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 23, 9, 0, 0, DateTimeKind.Utc);

    public CompleteExerciseCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
    }

    private CompleteExerciseCommandHandler CreateHandler() => new(
        _questRepository.Object,
        _hunterProgressionRepository.Object,
        _rankScoreLogRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _logger.Object);

    private static Quest BuildStartedQuestWithOneExercise(Guid userId, long xpReward = 24)
    {
        var quest = Quest.Create(userId, new DateTime(2026, 6, 23, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");
        var seed = new QuestExerciseSeed(
            Name: "Squat", ExerciseCatalogProviderId: "ex-1", Sets: 3, RepsMin: 8, RepsMax: 12,
            RestSeconds: 60, TargetRpe: "8", VideoUrl: null,
            XpReward: xpReward, StrengthXp: 2, AgilityXp: 0, EnduranceXp: 0, VitalityXp: 0, FocusXp: 0, WisdomXp: 1);
        quest.Start(UtcNow, [seed]);
        return quest;
    }

    // CA-001 US-064: conclusão completa (3/3) concede XP cheio e TotalXp é retornado.
    [Fact]
    public async Task FullCompletion_AwardsFullXpAndReturnsTotalXp()
    {
        var quest = BuildStartedQuestWithOneExercise(UserId, xpReward: 24);
        var exerciseId = quest.Exercises[0].Id;
        var progression = HunterProgression.Create(UserId);

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        var result = await CreateHandler().Handle(
            new CompleteExerciseCommand(quest.Id, exerciseId, SetsCompleted: 3), CancellationToken.None);

        result.Status.Should().Be("completed");
        result.XpEarned.Should().Be(24);
        result.TotalXp.Should().Be(24);
        result.EffectiveDifficulty.Should().Be(4);
        result.AttributeXpEarned.Strength.Should().Be(4);
        result.AttributeXpEarned.Wisdom.Should().Be(1);
        // US-130: buffer vazio → XP acumula internamente sem level up.
        result.AttributePointsGranted.Strength.Should().Be(0);
        result.AlreadyCompleted.Should().BeFalse();

        progression.TotalXp.Should().Be(24);
        progression.Strength.Should().Be(1);  // US-130: sem level up (buffer 0+4=4 < 10)
        progression.StrengthXp.Should().Be(4);
        progression.Wisdom.Should().Be(1);    // US-130: sem level up (buffer 0+1=1 < 10)
        progression.WisdomXp.Should().Be(1);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("xp_earned") &&
                    v.ToString()!.Contains("source=quest") &&
                    v.ToString()!.Contains("amount=24") &&
                    v.ToString()!.Contains($"questId={quest.Id}") &&
                    v.ToString()!.Contains($"questExerciseId={exerciseId}") &&
                    v.ToString()!.Contains($"userId={UserId}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // US-131: Sabedoria é sempre concedida em uma conclusão válida, mesmo se o snapshot não trouxer valor.
    [Fact]
    public async Task FullCompletion_AlwaysGrantsWisdom()
    {
        var quest = BuildStartedQuestWithOneExercise(UserId, xpReward: 24);
        var exercise = quest.Exercises[0];
        typeof(QuestExercise).GetProperty(nameof(QuestExercise.WisdomXp))!
            .SetValue(exercise, 0);
        var progression = HunterProgression.Create(UserId);

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        var result = await CreateHandler().Handle(
            new CompleteExerciseCommand(quest.Id, exercise.Id, SetsCompleted: 3), CancellationToken.None);

        result.AttributeXpEarned.Wisdom.Should().Be(1);
        // US-130: Sabedoria ganha XP interno mas não sobe de Level ainda (buffer 1 < 10).
        progression.Wisdom.Should().Be(1);
        progression.WisdomXp.Should().Be(1);
    }

    [Fact]
    public async Task FullCompletion_LogsLevelAndRankUps_WhenProgressionChanges()
    {
        var quest = BuildStartedQuestWithOneExercise(UserId, xpReward: 24);
        var exerciseId = quest.Exercises[0].Id;
        var progression = HunterProgression.CreateFromOnboarding(
            UserId,
            strength: 3,
            agility: 3,
            endurance: 3,
            vitality: 3,
            focus: 3,
            wisdom: 2);
        progression.AddXp(95, UtcNow);
        progression.AddAttributeXp(0, 0, 0, 0, 0, 9, externalMultiplier: 1.0m, utcNow: UtcNow);

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        await CreateHandler().Handle(
            new CompleteExerciseCommand(quest.Id, exerciseId, SetsCompleted: 3), CancellationToken.None);

        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("level_up") &&
                    v.ToString()!.Contains("newLevel=2") &&
                    v.ToString()!.Contains($"questId={quest.Id}") &&
                    v.ToString()!.Contains($"userId={UserId}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("rank_up") &&
                    v.ToString()!.Contains("newRank=D") &&
                    v.ToString()!.Contains($"questId={quest.Id}") &&
                    v.ToString()!.Contains($"userId={UserId}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // US-131 / CA-001: sabedoria tambem sobe de level quando o buffer interno chega em 10.
    [Fact]
    public async Task FullCompletion_LevelsUpWisdom_WhenWisdomBufferReachesTen()
    {
        var quest = BuildStartedQuestWithOneExercise(UserId, xpReward: 24);
        var exerciseId = quest.Exercises[0].Id;
        var progression = HunterProgression.Create(UserId);
        progression.AddAttributeXp(strength: 0, agility: 0, endurance: 0, vitality: 0, focus: 0, wisdom: 9,
            externalMultiplier: 1.0m, utcNow: UtcNow);

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        var result = await CreateHandler().Handle(
            new CompleteExerciseCommand(quest.Id, exerciseId, SetsCompleted: 3), CancellationToken.None);

        result.AttributeXpEarned.Wisdom.Should().Be(1);
        result.AttributeLevelUps.Should().Contain("wisdom");
        progression.Wisdom.Should().Be(2);
        progression.WisdomXp.Should().Be(0);
    }

    // CA-001 US-065: conclusão parcial (2/3) concede XP proporcional.
    [Fact]
    public async Task PartialCompletion_AwardsProportionalXp()
    {
        var quest = BuildStartedQuestWithOneExercise(UserId, xpReward: 24);
        var exerciseId = quest.Exercises[0].Id;
        var progression = HunterProgression.Create(UserId);

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        var result = await CreateHandler().Handle(
            new CompleteExerciseCommand(quest.Id, exerciseId, SetsCompleted: 2), CancellationToken.None);

        result.XpEarned.Should().Be(16); // 24 * (2/3) = 16
        result.TotalXp.Should().Be(16);
        result.AlreadyCompleted.Should().BeFalse();
        progression.TotalXp.Should().Be(16);
    }

    // CA-002 US-065: dor forte não eleva XP além do proporcional.
    [Fact]
    public async Task StrongPain_XpDoesNotExceedProportional()
    {
        var quest = BuildStartedQuestWithOneExercise(UserId, xpReward: 24);
        var exerciseId = quest.Exercises[0].Id;
        var progression = HunterProgression.Create(UserId);

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        var resultWithPain = await CreateHandler().Handle(
            new CompleteExerciseCommand(quest.Id, exerciseId, SetsCompleted: 3, StrongPainReported: true),
            CancellationToken.None);

        resultWithPain.XpEarned.Should().BeLessThanOrEqualTo(24);
        resultWithPain.XpEarned.Should().Be(24); // dor forte não corta XP proporcional puro
    }

    // RN-002/RN-006/9.1 US-058: idempotência - 2a chamada não duplica XP/atributos.
    [Fact]
    public async Task SecondCall_IsIdempotent_DoesNotDuplicateXpOrAttributes()
    {
        var quest = BuildStartedQuestWithOneExercise(UserId);
        var exerciseId = quest.Exercises[0].Id;
        var progression = HunterProgression.Create(UserId);

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        await CreateHandler().Handle(new CompleteExerciseCommand(quest.Id, exerciseId, SetsCompleted: 3), CancellationToken.None);
        var result = await CreateHandler().Handle(
            new CompleteExerciseCommand(quest.Id, exerciseId, SetsCompleted: 3), CancellationToken.None);

        result.AlreadyCompleted.Should().BeTrue();
        result.XpEarned.Should().Be(24);
        result.TotalXp.Should().Be(24);
        progression.TotalXp.Should().Be(24);
        // US-130: buffer acumulou apenas 4 XP (< 10), sem level up.
        progression.Strength.Should().Be(1);
        progression.StrengthXp.Should().Be(4);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("completed")]
    [InlineData("cancelled")]
    public async Task ThrowsConflict_WhenQuestIsNotInProgress(string status)
    {
        var quest = Quest.Create(UserId, new DateTime(2026, 6, 23, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");
        typeof(Quest).GetProperty(nameof(Quest.Status))!.SetValue(quest, status);
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(
            new CompleteExerciseCommand(quest.Id, Guid.NewGuid(), SetsCompleted: 3), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Code.Should().Be("QUEST_NOT_IN_PROGRESS");
    }

    [Fact]
    public async Task ThrowsNotFound_WhenExerciseDoesNotExist()
    {
        var quest = BuildStartedQuestWithOneExercise(UserId);
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(
            new CompleteExerciseCommand(quest.Id, Guid.NewGuid(), SetsCompleted: 3), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ThrowsNotFound_WhenQuestBelongsToAnotherUser()
    {
        var quest = BuildStartedQuestWithOneExercise(Guid.NewGuid());
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(
            new CompleteExerciseCommand(quest.Id, quest.Exercises[0].Id, SetsCompleted: 3), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ThrowsNotFound_WhenQuestDoesNotExist()
    {
        var questId = Guid.NewGuid();
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(questId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Quest?)null);

        var act = () => CreateHandler().Handle(
            new CompleteExerciseCommand(questId, Guid.NewGuid(), SetsCompleted: 3), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
