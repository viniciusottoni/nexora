using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Commands.ChangeTrainingType;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Quests;

public class ChangeTrainingTypeCommandHandlerTests
{
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly Mock<IHunterProgressionRepository> _progressionRepository = new();
    private readonly Mock<IWorkoutGeneratorService> _workoutGenerator = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid QuestId = Guid.NewGuid();

    private const string WorkoutJson = """
    {
      "title": "Daily Quest", "description": "Full body",
      "durationMinutes": 30,
      "exercises": [{ "name": "Squat", "sets": 3, "repsMin": 10 }]
    }
    """;

    public ChangeTrainingTypeCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(d => d.UtcNow).Returns(new DateTime(2026, 6, 28, 10, 0, 0, DateTimeKind.Utc));

        var user = User.Create("hunter@awaken.app", "hash", "Hunter", "pt-BR");
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _workoutGenerator
            .Setup(g => g.GenerateWorkoutJsonAsync(
                UserId, "pt-BR", It.IsAny<string>(), It.IsAny<UserProfile?>(), It.IsAny<HunterProgression?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutGenerationResult(
                WorkoutJson, IsPersonalized: true, "catalog_rules", "{}"));
    }

    private ChangeTrainingTypeCommandHandler CreateHandler() => new(
        _questRepository.Object,
        _userRepository.Object,
        _userProfileRepository.Object,
        _progressionRepository.Object,
        _workoutGenerator.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object);

    private Quest BuildPendingQuest()
    {
        var quest = Quest.Create(UserId, DateTime.UtcNow.Date, "pt-BR", "idem");
        quest.AssignWorkout(WorkoutJson, DateTime.UtcNow);
        return quest;
    }

    // â”€â”€ CA-001: alterar para regeneraÃ§Ã£o â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task CA001_ChangesToRegeneration_ReturnsPreviewWithRegenerationType()
    {
        var quest = BuildPendingQuest();
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler()
            .Handle(new ChangeTrainingTypeCommand(quest.Id, "regeneration", null), CancellationToken.None);

        result.TrainingType.Should().Be("regeneration");
        result.CanChangeTrainingType.Should().BeTrue();
        result.Workout.Should().NotBeNull();
    }

    // â”€â”€ CA-002: alterar para programa â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task CA002_ChangesToSaitamaPath_ReturnsPreviewWithProgramType()
    {
        var quest = BuildPendingQuest();
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler()
            .Handle(new ChangeTrainingTypeCommand(quest.Id, "program", "saitama_path"), CancellationToken.None);

        result.TrainingType.Should().Be("program");
        result.Workout.Should().NotBeNull();
        result.Workout!.Title.Should().Contain("Saitama");
    }

    [Fact]
    public async Task CA002_ChangesToPerfect2_ReturnsPreviewWithProgramType()
    {
        var quest = BuildPendingQuest();
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler()
            .Handle(new ChangeTrainingTypeCommand(quest.Id, "program", "perfect_2"), CancellationToken.None);

        result.TrainingType.Should().Be("program");
        result.Workout!.Title.Should().Contain("Perfect 2");
    }

    [Fact]
    public async Task ChangesToPersonalizedIndividual_CallsWorkoutGenerator()
    {
        var quest = BuildPendingQuest();
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var profile = Awaken.Domain.Entities.Onboarding.UserProfile.Create(UserId);
        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _progressionRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Awaken.Domain.Entities.Progression.HunterProgression?)null);

        var result = await CreateHandler()
            .Handle(new ChangeTrainingTypeCommand(quest.Id, "personalized_individual", null), CancellationToken.None);

        _workoutGenerator.Verify(
            g => g.GenerateWorkoutJsonAsync(UserId, "pt-BR", It.IsAny<string>(), It.IsAny<UserProfile?>(), It.IsAny<HunterProgression?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        result.TrainingType.Should().Be("personalized_individual");
    }

    // â”€â”€ RN-001: alteraÃ§Ã£o bloqueada apÃ³s inÃ­cio â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task RN001_Throws_ConflictException_WhenQuestIsInProgress()
    {
        var quest = BuildPendingQuest();
        quest.Start(DateTime.UtcNow, Array.Empty<QuestExerciseSeed>());
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        await CreateHandler()
            .Invoking(h => h.Handle(
                new ChangeTrainingTypeCommand(quest.Id, "regeneration", null), CancellationToken.None))
            .Should().ThrowAsync<ConflictException>()
            .Where(e => e.Code == "QUEST_ALREADY_STARTED");
    }

    [Fact]
    public async Task RN001_Throws_ConflictException_WhenQuestIsCompleted()
    {
        var quest = BuildPendingQuest();
        quest.Start(DateTime.UtcNow, Array.Empty<QuestExerciseSeed>());
        quest.Complete(xpAwarded: 100, DateTime.UtcNow);
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        await CreateHandler()
            .Invoking(h => h.Handle(
                new ChangeTrainingTypeCommand(quest.Id, "regeneration", null), CancellationToken.None))
            .Should().ThrowAsync<ConflictException>()
            .Where(e => e.Code == "QUEST_ALREADY_STARTED");
    }

    // â”€â”€ Posse e existÃªncia â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Throws_NotFoundException_WhenQuestDoesNotExist()
    {
        _questRepository.Setup(r => r.GetByIdAsync(QuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Quest?)null);

        await CreateHandler()
            .Invoking(h => h.Handle(
                new ChangeTrainingTypeCommand(QuestId, "regeneration", null), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_UnauthorizedException_WhenQuestBelongsToAnotherUser()
    {
        var otherQuest = Quest.Create(Guid.NewGuid(), DateTime.UtcNow.Date, "pt-BR", "other");
        otherQuest.AssignWorkout(WorkoutJson, DateTime.UtcNow);
        _questRepository.Setup(r => r.GetByIdAsync(otherQuest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherQuest);

        await CreateHandler()
            .Invoking(h => h.Handle(
                new ChangeTrainingTypeCommand(otherQuest.Id, "regeneration", null), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>()
            .Where(e => e.Code == "QUEST_NOT_OWNED");
    }

    // â”€â”€ US-052: persiste alteraÃ§Ã£o e chama unitOfWork â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task PersistsChange_AndCallsSaveChanges()
    {
        var quest = BuildPendingQuest();
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        await CreateHandler()
            .Handle(new ChangeTrainingTypeCommand(quest.Id, "regeneration", null), CancellationToken.None);

        _questRepository.Verify(r => r.Update(quest), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        quest.TrainingType.Should().Be("regeneration");
    }
}

