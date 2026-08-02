using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Queries.ValidateTrainingTypeChange;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Quests;

public class ValidateTrainingTypeChangeQueryHandlerTests
{
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly Mock<IHunterProgressionRepository> _progressionRepository = new();
    private readonly Mock<IWorkoutGeneratorService> _workoutGenerator = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid QuestId = Guid.NewGuid();

    private const string WorkoutJson = """
    {
      "title": "Daily Quest", "description": "Full body",
      "durationMinutes": 30,
      "exercises": [{ "name": "Squat", "sets": 3, "repsMin": 10 }]
    }
    """;

    public ValidateTrainingTypeChangeQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);

        var user = User.Create("hunter@awaken.app", "hash", "Hunter", "pt-BR");
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _workoutGenerator
            .Setup(g => g.GenerateWorkoutJsonAsync(
                UserId, "pt-BR", It.IsAny<string>(), It.IsAny<UserProfile?>(), It.IsAny<HunterProgression?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutGenerationResult(
                WorkoutJson, IsPersonalized: true, "catalog_rules", "{}"));
    }

    private ValidateTrainingTypeChangeQueryHandler CreateHandler() => new(
        _questRepository.Object,
        _userRepository.Object,
        _userProfileRepository.Object,
        _progressionRepository.Object,
        _workoutGenerator.Object,
        _currentUserService.Object);

    private Quest BuildPendingQuest()
    {
        var quest = Quest.Create(UserId, DateTime.UtcNow.Date, "pt-BR", "idem");
        quest.AssignWorkout(WorkoutJson, DateTime.UtcNow);
        return quest;
    }

    // â”€â”€ CA-001: tipo valido (regeneracao) â€” 20 min â†’ 80 XP â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task CA001_ReturnsValid_WithRecalculatedXpAndDuration_ForRegeneration()
    {
        var quest = BuildPendingQuest();
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(
            new ValidateTrainingTypeChangeQuery(quest.Id, "regeneration", null), CancellationToken.None);

        result.Valid.Should().BeTrue();
        result.EstimatedDurationMinutes.Should().Be(20);
        result.EstimatedXp.Should().Be(80);
    }

    [Fact]
    public async Task CA001_ReturnsValid_ForSaitamaPath()
    {
        var quest = BuildPendingQuest();
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(
            new ValidateTrainingTypeChangeQuery(quest.Id, "program", "saitama_path"), CancellationToken.None);

        result.Valid.Should().BeTrue();
        result.EstimatedDurationMinutes.Should().Be(60);
        result.EstimatedXp.Should().Be(240);
    }

    [Fact]
    public async Task CA001_ReturnsValid_ForPerfect2()
    {
        var quest = BuildPendingQuest();
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(
            new ValidateTrainingTypeChangeQuery(quest.Id, "program", "perfect_2"), CancellationToken.None);

        result.Valid.Should().BeTrue();
        result.EstimatedDurationMinutes.Should().Be(45);
        result.EstimatedXp.Should().Be(180);
    }

    // â”€â”€ Dry-run: nunca persiste â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task DoesNotPersist_NeverCallsUpdate()
    {
        var quest = BuildPendingQuest();
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        await CreateHandler().Handle(
            new ValidateTrainingTypeChangeQuery(quest.Id, "regeneration", null), CancellationToken.None);

        _questRepository.Verify(r => r.Update(It.IsAny<Quest>()), Times.Never);
        quest.TrainingType.Should().Be("personalized_individual");
    }

    // â”€â”€ RN-001: quest iniciada bloqueia validacao â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task RN001_Throws_ConflictException_WhenQuestInProgress()
    {
        var quest = BuildPendingQuest();
        quest.Start(DateTime.UtcNow, Array.Empty<QuestExerciseSeed>());
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        await CreateHandler()
            .Invoking(h => h.Handle(
                new ValidateTrainingTypeChangeQuery(quest.Id, "regeneration", null), CancellationToken.None))
            .Should().ThrowAsync<ConflictException>()
            .Where(e => e.Code == "QUEST_ALREADY_STARTED");
    }

    [Fact]
    public async Task RN001_Throws_ConflictException_WhenQuestCompleted()
    {
        var quest = BuildPendingQuest();
        quest.Start(DateTime.UtcNow, Array.Empty<QuestExerciseSeed>());
        quest.Complete(xpAwarded: 100, DateTime.UtcNow);
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        await CreateHandler()
            .Invoking(h => h.Handle(
                new ValidateTrainingTypeChangeQuery(quest.Id, "regeneration", null), CancellationToken.None))
            .Should().ThrowAsync<ConflictException>()
            .Where(e => e.Code == "QUEST_ALREADY_STARTED");
    }

    // â”€â”€ RN-003: programa invalido â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task RN003_Throws_ConflictException_WhenProgramIdUnknown()
    {
        var quest = BuildPendingQuest();
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        await CreateHandler()
            .Invoking(h => h.Handle(
                new ValidateTrainingTypeChangeQuery(quest.Id, "program", "unknown_program"), CancellationToken.None))
            .Should().ThrowAsync<ConflictException>()
            .Where(e => e.Code == "INVALID_PROGRAM_ID");
    }

    // â”€â”€ Posse e existencia â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Throws_UnauthorizedException_WhenQuestBelongsToAnotherUser()
    {
        var otherQuest = Quest.Create(Guid.NewGuid(), DateTime.UtcNow.Date, "pt-BR", "other");
        otherQuest.AssignWorkout(WorkoutJson, DateTime.UtcNow);
        _questRepository.Setup(r => r.GetByIdAsync(otherQuest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherQuest);

        await CreateHandler()
            .Invoking(h => h.Handle(
                new ValidateTrainingTypeChangeQuery(otherQuest.Id, "regeneration", null), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>()
            .Where(e => e.Code == "QUEST_NOT_OWNED");
    }

    [Fact]
    public async Task Throws_NotFoundException_WhenQuestDoesNotExist()
    {
        _questRepository.Setup(r => r.GetByIdAsync(QuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Quest?)null);

        await CreateHandler()
            .Invoking(h => h.Handle(
                new ValidateTrainingTypeChangeQuery(QuestId, "regeneration", null), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}

