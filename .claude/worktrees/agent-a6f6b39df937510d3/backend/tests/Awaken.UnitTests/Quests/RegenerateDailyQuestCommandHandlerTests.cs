using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Commands.RegenerateDailyQuest;
using Awaken.Application.Quests.Common;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using Awaken.Domain.Services.Quests;
using FluentAssertions;
using Moq;
using Awaken.Domain.Entities.Progression;

namespace Awaken.UnitTests.Quests;

public class RegenerateDailyQuestCommandHandlerTests
{
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly Mock<IHunterProgressionRepository> _hunterProgressionRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IWorkoutGeneratorService> _workoutGeneratorService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUserDateService> _userDateService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 6, 22);
    private static readonly DateTime QuestDateUtc = new(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc);

    private const string WorkoutJson = """
    {
      "title": "Daily Quest",
      "description": "Full body",
      "durationMinutes": 30,
      "exercises": [
        { "name": "Squat", "sets": 3, "reps": 12, "restSeconds": 60 }
      ]
    }
    """;

    public RegenerateDailyQuestCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.TodayUtc).Returns(Today);
        _userDateService.Setup(s => s.TodayLocal).Returns(Today);
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(BuildUser());
        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProfile.Create(UserId, goal: "gain_muscle"));
        _workoutGeneratorService
            .Setup(s => s.GenerateWorkoutJsonAsync(UserId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserProfile?>(), It.IsAny<HunterProgression?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutGenerationResult(
                WorkoutJson, IsPersonalized: true, GenerationMethod: "catalog_rules", AppliedFiltersJson: "{}"));
    }

    // US-230: mecânica de regeneração foi extraída para IQuestRegenerationService
    // (compartilhada com ReforgeScrollEffectHandler) — instancia a implementação
    // real sobre os mesmos mocks para preservar a cobertura de comportamento
    // (ex.: reuso do profile snapshot).
    private RegenerateDailyQuestCommandHandler CreateHandler()
    {
        var regenerationService = new QuestRegenerationService(
            _questRepository.Object,
            _userRepository.Object,
            _userProfileRepository.Object,
            _hunterProgressionRepository.Object,
            _workoutGeneratorService.Object,
            _userDateService.Object,
            _dateTimeService.Object);

        return new RegenerateDailyQuestCommandHandler(
            _questRepository.Object,
            _inventoryRepository.Object,
            regenerationService,
            _currentUserService.Object,
            _dateTimeService.Object,
            _userDateService.Object,
            _unitOfWork.Object);
    }

    private static User BuildUser()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(DateTime.UtcNow.AddDays(7));
        return user;
    }

    private static Quest BuildQuestWithRegenerations(int regenerationCount)
    {
        var quest = Quest.Create(UserId, QuestDateUtc, "pt-BR", "key");
        quest.AssignWorkout(WorkoutJson, DateTime.UtcNow);
        for (var i = 0; i < regenerationCount; i++)
        {
            quest.Regenerate(WorkoutJson, isPersonalized: true, viaReforgeScroll: false, DateTime.UtcNow);
        }
        return quest;
    }

    private void SetupExistingQuest(Quest quest) =>
        _questRepository
            .Setup(r => r.GetByUserIdAndDateAsync(UserId, "daily", QuestDateUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

    [Fact]
    public async Task CA001_RegeneratesQuest_WhenWithinDailyLimit()
    {
        var quest = BuildQuestWithRegenerations(0);
        SetupExistingQuest(quest);

        var result = await CreateHandler().Handle(new RegenerateDailyQuestCommand(), CancellationToken.None);

        result.RegenerationsUsed.Should().Be(1);
        quest.GenerationReason.Should().Be("regeneration");
        _questRepository.Verify(r => r.Update(quest), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _inventoryRepository.Verify(
            r => r.GetByUserIdAndItemKeyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RN004_Regenerate_ReusesSameProfileSnapshot_PreservingLimitationsAndPains()
    {
        var quest = BuildQuestWithRegenerations(0);
        SetupExistingQuest(quest);
        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProfile.Create(UserId, physicalPains: ["lower_back"]));
        string? capturedProfileJson = null;
        _workoutGeneratorService
            .Setup(s => s.GenerateWorkoutJsonAsync(UserId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserProfile?>(), It.IsAny<HunterProgression?>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, string, UserProfile?, HunterProgression?, CancellationToken>((_, _, json, _, _, _) => capturedProfileJson = json)
            .ReturnsAsync(new WorkoutGenerationResult(
                WorkoutJson, IsPersonalized: true, GenerationMethod: "catalog_rules", AppliedFiltersJson: "{}"));

        await CreateHandler().Handle(new RegenerateDailyQuestCommand(), CancellationToken.None);

        capturedProfileJson.Should().Contain("lower_back");
        quest.ProfileSnapshotJson.Should().Contain("lower_back");
    }

    [Fact]
    public async Task CA002_ThrowsConflict_WhenDailyLimitReachedWithoutReforgeScroll()
    {
        var quest = BuildQuestWithRegenerations(QuestRegenerationPolicy.DailyFreeLimit);
        SetupExistingQuest(quest);

        var act = () => CreateHandler().Handle(new RegenerateDailyQuestCommand(), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Code.Should().Be("REGENERATION_LIMIT_REACHED");
        _workoutGeneratorService.Verify(
            s => s.GenerateWorkoutJsonAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserProfile?>(), It.IsAny<HunterProgression?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Obs_ConsumesReforgeScroll_AndRegenerates_WhenLimitReachedAndScrollAvailable()
    {
        var quest = BuildQuestWithRegenerations(QuestRegenerationPolicy.DailyFreeLimit);
        SetupExistingQuest(quest);
        var scroll = InventoryItem.Create(UserId, ItemKeys.ReforgeScroll, quantity: 1);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.ReforgeScroll, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scroll);

        var result = await CreateHandler().Handle(
            new RegenerateDailyQuestCommand(UseReforgeScroll: true), CancellationToken.None);

        scroll.Quantity.Should().Be(0);
        result.RegenerationsUsed.Should().Be(QuestRegenerationPolicy.DailyFreeLimit);
        quest.GenerationMethod.Should().Contain("reforge_scroll");
        _inventoryRepository.Verify(r => r.Update(scroll), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Obs_ThrowsConflict_WhenScrollRequestedButNotAvailable()
    {
        var quest = BuildQuestWithRegenerations(QuestRegenerationPolicy.DailyFreeLimit);
        SetupExistingQuest(quest);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.ReforgeScroll, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var act = () => CreateHandler().Handle(
            new RegenerateDailyQuestCommand(UseReforgeScroll: true), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Code.Should().Be("REFORGE_SCROLL_NOT_AVAILABLE");
    }

    [Fact]
    public async Task ThrowsNotFound_WhenNoQuestGeneratedToday()
    {
        _questRepository
            .Setup(r => r.GetByUserIdAndDateAsync(UserId, "daily", QuestDateUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Quest?)null);

        var act = () => CreateHandler().Handle(new RegenerateDailyQuestCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // US-238/US-240: a regeneração também atualiza o dia resolvido e o snapshot do
    // blueprint (RN-008/US-049), para não deixar a auditoria/rotação desatualizada.
    [Fact]
    public async Task US240_AssignsResolvedProgramDayAndBlueprint_OnRegeneration()
    {
        var quest = BuildQuestWithRegenerations(0);
        SetupExistingQuest(quest);
        _workoutGeneratorService
            .Setup(s => s.GenerateWorkoutJsonAsync(UserId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserProfile?>(), It.IsAny<HunterProgression?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutGenerationResult(
                WorkoutJson, IsPersonalized: true, GenerationMethod: "catalog_rules", AppliedFiltersJson: "{}",
                ResolvedProgramKey: "abc", ResolvedDayKey: "C", ResolvedDayIndex: 3, SplitMapVersion: "v1",
                DailyWorkoutBlueprintJson: "{\"programKey\":\"abc\"}"));

        await CreateHandler().Handle(new RegenerateDailyQuestCommand(), CancellationToken.None);

        quest.ResolvedProgramKey.Should().Be("abc");
        quest.ResolvedDayKey.Should().Be("C");
        quest.ResolvedDayIndex.Should().Be(3);
        quest.SplitMapVersion.Should().Be("v1");
        quest.DailyWorkoutBlueprintJson.Should().Be("{\"programKey\":\"abc\"}");
    }
}
