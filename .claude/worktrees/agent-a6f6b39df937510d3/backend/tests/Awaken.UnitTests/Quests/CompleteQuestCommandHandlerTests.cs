using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Commands.CompleteQuest;
using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Entities.Training;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Quests;

public class CompleteQuestCommandHandlerTests
{
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<IQuestLogRepository> _questLogRepository = new();
    private readonly Mock<IHunterProgressionRepository> _hunterProgressionRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IRankScoreLogRepository> _rankScoreLogRepository = new();
    private readonly Mock<IItemActiveEffectRepository> _itemActiveEffectRepository = new();
    private readonly Mock<IMuscleRecoveryStateRepository> _muscleRecoveryStateRepository = new();
    private readonly Mock<IExerciseCatalogRepository> _exerciseCatalogRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<CompleteQuestCommandHandler>> _logger = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 24, 9, 0, 0, DateTimeKind.Utc);

    public CompleteQuestCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
        _questLogRepository.Setup(r => r.GetByQuestIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuestLog?)null);

        // US-230: sem Poção de Foco/Amuleto de Retorno ativos por padrão —
        // testes específicos sobrescrevem este setup.
        _itemActiveEffectRepository
            .Setup(r => r.GetActiveByUserAndTypeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private CompleteQuestCommandHandler CreateHandler() => new(
        _questRepository.Object,
        _questLogRepository.Object,
        _hunterProgressionRepository.Object,
        _inventoryRepository.Object,
        _rankScoreLogRepository.Object,
        _itemActiveEffectRepository.Object,
        _muscleRecoveryStateRepository.Object,
        _exerciseCatalogRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _logger.Object);

    private static Quest BuildQuestWithCompletedExercise(
        Guid userId, string type = "daily", long xpReward = 24)
    {
        var quest = Quest.Create(userId, new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");
        if (type != "daily")
            typeof(Quest).GetProperty(nameof(Quest.Type))!.SetValue(quest, type);

        var seed = new QuestExerciseSeed(
            Name: "Squat", ExerciseCatalogProviderId: "ex-1", Sets: 3, RepsMin: 8, RepsMax: 12,
            RestSeconds: 60, TargetRpe: "8", VideoUrl: null,
            XpReward: xpReward, StrengthXp: 2, AgilityXp: 0, EnduranceXp: 0, VitalityXp: 0, FocusXp: 0, WisdomXp: 1);
        quest.Start(UtcNow, [seed]);
        quest.Exercises[0].MarkCompleted(UtcNow, calculatedXp: xpReward);
        return quest;
    }

    // US-239: catálogo mínimo usado para resolver grupos musculares/movementFamily
    // a partir do ExerciseCatalogProviderId do QuestExercise.
    private static ExerciseCatalog BuildCatalog(
        string providerExerciseId, int difficultyRank, int impactLevel,
        IReadOnlyList<string> primaryMuscleGroups, IReadOnlyList<string>? secondaryMuscleGroups = null) =>
        ExerciseCatalog.Create(
            new ExerciseCatalogSnapshot(
                RawImportId: null,
                ProviderName: "local_files",
                ProviderExerciseId: providerExerciseId,
                ProviderVersion: null,
                NamePtBr: "Agachamento",
                NameOriginal: "Squat",
                Slug: "agachamento",
                DescriptionPtBr: null,
                InstructionsPtBr: ["Passo 1"],
                InstructionsOriginal: ["Step 1"],
                TipsPtBr: [],
                ExerciseType: "strength",
                MovementPattern: "squat",
                MovementFamily: "squat_family",
                Mechanic: "compound",
                ForceType: "push",
                PlaneOfMotion: "sagittal",
                Laterality: "bilateral",
                BodyPosition: "standing",
                BenchAngle: null,
                EquipmentCategory: "free_weight",
                LoadType: "free_weight",
                PrimaryRegion: "lower_body",
                DifficultyLevel: "intermediate",
                DifficultyRank: difficultyRank,
                TechnicalComplexity: 3,
                ImpactLevel: impactLevel,
                Environment: "gym",
                RequiredEquipment: ["barbell"],
                PrimaryMuscleGroups: primaryMuscleGroups.ToList(),
                SecondaryMuscleGroups: (secondaryMuscleGroups ?? []).ToList(),
                BodyParts: ["legs"],
                JointStressTags: [],
                ContraindicationTags: [],
                LimitationBlockTags: [],
                PainBlockTags: [],
                GoalTags: [],
                RiskTags: [],
                AccessibilityTags: [],
                TaxonomySignals: [],
                MinExperienceLevel: "intermediate",
                SuitableForSedentary: false,
                SuitableForBeginner: false,
                SuitableForIntermediate: true,
                SuitableForAdvanced: true,
                IsCompound: true,
                IsUnilateral: false,
                IsAssisted: false,
                IsWeighted: true,
                RegressionExerciseIds: [],
                ProgressionExerciseIds: [],
                RelatedExerciseIds: [],
                VideoUrl: null,
                ImageUrl: null,
                GifUrl: "https://cdn.awaken.app/0001/360.gif",
                MediaLicenseInfo: null,
                SanitizationStatus: "pending_review",
                IsApprovedForWorkoutGeneration: false,
                Confidence: "high"),
            UtcNow);

    private QuestLog? _capturedLog;

    private void SetupSaveCapturesLog()
    {
        _questLogRepository
            .Setup(r => r.AddAsync(It.IsAny<QuestLog>(), It.IsAny<CancellationToken>()))
            .Callback<QuestLog, CancellationToken>((log, _) => _capturedLog = log)
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task CA001_CompletesDaily_ConsolidatesXpAttributesAndStreak()
    {
        SetupSaveCapturesLog();
        var quest = BuildQuestWithCompletedExercise(UserId);
        var progression = HunterProgression.Create(UserId);

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        var result = await CreateHandler().Handle(new CompleteQuestCommand(quest.Id), CancellationToken.None);

        result.Status.Should().Be("completed");
        result.QuestType.Should().Be("daily");
        result.XpEarned.Should().Be(24);
        result.AttributeXpEarned.Strength.Should().Be(4);
        result.AttributeXpEarned.Wisdom.Should().Be(1);
        // US-130: level-ups reais registrados no QuestExercise (0 neste teste — sem SetAttributeLevelUps).
        result.AttributePointsGranted.Strength.Should().Be(0);
        result.ItemsEarned.Should().BeEmpty();

        quest.Status.Should().Be("completed");
        quest.XpAwarded.Should().Be(24);
        progression.CurrentStreakDays.Should().Be(1);

        _questLogRepository.Verify(r => r.AddAsync(It.IsAny<QuestLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RN007_CompletesDungeon_GrantsItemAndCreatesInventoryRecord()
    {
        SetupSaveCapturesLog();
        var quest = BuildQuestWithCompletedExercise(UserId, type: "dungeon");
        var progression = HunterProgression.Create(UserId);

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        _inventoryRepository.Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.DungeonStone, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var result = await CreateHandler().Handle(new CompleteQuestCommand(quest.Id), CancellationToken.None);

        result.QuestType.Should().Be("dungeon");
        result.ItemsEarned.Should().ContainSingle().Which.Should().Be(ItemKeys.DungeonStone);
        _inventoryRepository.Verify(
            r => r.AddAsync(It.Is<InventoryItem>(i => i.ItemKey == ItemKeys.DungeonStone && i.Quantity == 1), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RN009_CompletesRaid_UsesRaidQuestType()
    {
        SetupSaveCapturesLog();
        var quest = BuildQuestWithCompletedExercise(UserId, type: "raid");

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(HunterProgression.Create(UserId));

        var result = await CreateHandler().Handle(new CompleteQuestCommand(quest.Id), CancellationToken.None);

        result.QuestType.Should().Be("raid");
        result.ItemsEarned.Should().BeEmpty();
    }

    [Fact]
    public async Task CA002_SecondCall_IsIdempotent_DoesNotDuplicateRewardOrLog()
    {
        SetupSaveCapturesLog();
        var quest = BuildQuestWithCompletedExercise(UserId, type: "dungeon");
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(HunterProgression.Create(UserId));
        _inventoryRepository.Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.DungeonStone, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var first = await CreateHandler().Handle(new CompleteQuestCommand(quest.Id), CancellationToken.None);

        // 2a chamada: quest ja esta completed, log ja existe no repositorio.
        _questLogRepository.Setup(r => r.GetByQuestIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_capturedLog);

        var second = await CreateHandler().Handle(new CompleteQuestCommand(quest.Id), CancellationToken.None);

        second.XpEarned.Should().Be(first.XpEarned);
        second.ItemsEarned.Should().BeEquivalentTo(first.ItemsEarned);
        _questLogRepository.Verify(r => r.AddAsync(It.IsAny<QuestLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _inventoryRepository.Verify(r => r.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("cancelled")]
    public async Task RN001_RN002_ThrowsConflict_WhenQuestNotCompletable(string status)
    {
        var quest = Quest.Create(UserId, new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");
        typeof(Quest).GetProperty(nameof(Quest.Status))!.SetValue(quest, status);
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(new CompleteQuestCommand(quest.Id), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Code.Should().Be("QUEST_NOT_COMPLETABLE");
    }

    [Fact]
    public async Task ThrowsNotFound_WhenQuestDoesNotExist()
    {
        var questId = Guid.NewGuid();
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(questId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Quest?)null);

        var act = () => CreateHandler().Handle(new CompleteQuestCommand(questId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ThrowsNotFound_WhenQuestBelongsToAnotherUser()
    {
        var quest = BuildQuestWithCompletedExercise(Guid.NewGuid());
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(new CompleteQuestCommand(quest.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // US-069 / CA-001: bônus de RankScore é aplicado ao atingir o marco de 7 dias.
    [Fact]
    public async Task US069_AppliesStreakMilestoneBonus_WhenStreakHits7Days()
    {
        SetupSaveCapturesLog();
        var quest = BuildQuestWithCompletedExercise(UserId);
        var progression = HunterProgression.Create(UserId);

        // Simula 6 dias anteriores de streak para que a conclusão desta quest leve a 7.
        for (var i = 0; i < 6; i++)
        {
            progression.UpdateStreakAfterQuestCompletion(UtcNow.AddDays(-(6 - i)));
        }
        progression.CurrentStreakDays.Should().Be(6);
        var rankScoreBefore = progression.RankScore;

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        await CreateHandler().Handle(new CompleteQuestCommand(quest.Id), CancellationToken.None);

        progression.CurrentStreakDays.Should().Be(7);
        progression.StreakRankScoreBonus.Should().Be(1);
        progression.RankScore.Should().Be(rankScoreBefore + 1);
    }

    // US-069: sem marco atingido, StreakRankScoreBonus permanece inalterado.
    [Fact]
    public async Task US069_NoStreakBonus_WhenNoMilestoneHit()
    {
        SetupSaveCapturesLog();
        var quest = BuildQuestWithCompletedExercise(UserId);
        var progression = HunterProgression.Create(UserId);
        // streak de 2 dias → dia 3, sem marco

        progression.UpdateStreakAfterQuestCompletion(UtcNow.AddDays(-2));
        progression.UpdateStreakAfterQuestCompletion(UtcNow.AddDays(-1));

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        await CreateHandler().Handle(new CompleteQuestCommand(quest.Id), CancellationToken.None);

        progression.CurrentStreakDays.Should().Be(3);
        progression.StreakRankScoreBonus.Should().Be(0);
    }

    [Fact]
    public async Task US136_LogsAttributeLevelUp_WhenStrengthLeveledUp()
    {
        SetupSaveCapturesLog();
        var quest = BuildQuestWithCompletedExercise(UserId);
        quest.Exercises[0].SetAttributeLevelUps(1, 0, 0, 0, 0, 0); // strength level-up
        var progression = HunterProgression.Create(UserId);

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        await CreateHandler().Handle(new CompleteQuestCommand(quest.Id), CancellationToken.None);

        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("attribute_level_up") &&
                    v.ToString()!.Contains("attribute=strength")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task US138_LogsItemEarned_WhenDungeonGrantsItem()
    {
        SetupSaveCapturesLog();
        var quest = BuildQuestWithCompletedExercise(UserId, type: "dungeon");
        var progression = HunterProgression.Create(UserId);

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        _inventoryRepository.Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.DungeonStone, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        await CreateHandler().Handle(new CompleteQuestCommand(quest.Id), CancellationToken.None);

        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("item_earned") &&
                    v.ToString()!.Contains($"itemKey={ItemKeys.DungeonStone}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // US-230/P0-1: Poção de Foco ativa aplica +25% sobre o TOTAL do treino
    // (não sobre o exercício isolado) e é consumida (não reaplicável).
    [Fact]
    public async Task US230_AppliesXpBoost_OverQuestTotal_AndConsumesEffect()
    {
        SetupSaveCapturesLog();
        var quest = BuildQuestWithCompletedExercise(UserId, xpReward: 100);
        var progression = HunterProgression.Create(UserId);
        var totalXpBefore = progression.TotalXp;

        var boost = ItemActiveEffect.Create(
            UserId, ItemKeys.FocusPotion, ItemEffectTypes.XpBoost,
            activatedAtUtc: UtcNow.AddHours(-1), expiresAtUtc: UtcNow.AddDays(1),
            xpBoostMultiplier: 0.25m);

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        _itemActiveEffectRepository
            .Setup(r => r.GetActiveByUserAndTypeAsync(UserId, ItemEffectTypes.XpBoost, It.IsAny<CancellationToken>()))
            .ReturnsAsync([boost]);

        var result = await CreateHandler().Handle(new CompleteQuestCommand(quest.Id), CancellationToken.None);

        result.XpEarned.Should().Be(125); // 100 + 25% de bônus
        // Nota: o XP base (100) já teria sido creditado por
        // CompleteExerciseCommandHandler na conclusão do exercício (fora do
        // escopo deste teste, que simula o exercício já completo direto via
        // MarkCompleted) — aqui só o BÔNUS do boost é creditado por
        // CompleteQuestCommandHandler, consistente com o design (P0-1).
        progression.TotalXp.Should().Be(totalXpBefore + 25);
        boost.IsActive.Should().BeFalse();
        _itemActiveEffectRepository.Verify(r => r.Update(boost), Times.Once);
    }

    // US-230: Amuleto de Retorno ativo restaura o streak perdido em vez de
    // reiniciar do zero, e é consumido.
    [Fact]
    public async Task US230_RestoresStreak_WhenReturnAmuletActive()
    {
        SetupSaveCapturesLog();
        var quest = BuildQuestWithCompletedExercise(UserId);
        var progression = HunterProgression.Create(UserId);

        var recovery = ItemActiveEffect.Create(
            UserId, ItemKeys.ReturnAmulet, ItemEffectTypes.StreakRecovery,
            activatedAtUtc: UtcNow.AddHours(-1), expiresAtUtc: UtcNow.AddDays(1),
            streakDaysToRestore: 5);

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        _itemActiveEffectRepository
            .Setup(r => r.GetActiveByUserAndTypeAsync(UserId, ItemEffectTypes.StreakRecovery, It.IsAny<CancellationToken>()))
            .ReturnsAsync([recovery]);

        await CreateHandler().Handle(new CompleteQuestCommand(quest.Id), CancellationToken.None);

        progression.CurrentStreakDays.Should().Be(6); // 5 restaurados + 1 de hoje
        recovery.IsActive.Should().BeFalse();
        _itemActiveEffectRepository.Verify(r => r.Update(recovery), Times.Once);
    }

    // US-239: exercício concluído com catálogo resolvido (pesado — difficultyRank>=3)
    // registra sessão em IMuscleRecoveryStateRepository para o grupo trabalhado.
    [Fact]
    public async Task US239_UpdatesMuscleRecoveryState_ForGroupsOfCompletedExercise()
    {
        SetupSaveCapturesLog();
        var quest = BuildQuestWithCompletedExercise(UserId);
        var progression = HunterProgression.Create(UserId);

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        var catalog = BuildCatalog("ex-1", difficultyRank: 3, impactLevel: 1, primaryMuscleGroups: ["quadriceps"]);
        _exerciseCatalogRepository
            .Setup(r => r.GetByProviderExerciseIdAsync("ex-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalog);
        _muscleRecoveryStateRepository
            .Setup(r => r.GetByUserAndMuscleGroupAsync(UserId, "quadriceps", It.IsAny<CancellationToken>()))
            .ReturnsAsync((MuscleRecoveryState?)null);

        await CreateHandler().Handle(new CompleteQuestCommand(quest.Id), CancellationToken.None);

        _muscleRecoveryStateRepository.Verify(
            r => r.AddAsync(
                It.Is<MuscleRecoveryState>(s => s.MuscleGroup == "quadriceps" && s.LastIntensity == "heavy"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // US-239/RN-008 (fallback): exercício sem catálogo resolvido (ExerciseCatalogProviderId
    // não bate com nenhum ExerciseCatalog) não gera atualização de recuperação nem derruba o fluxo.
    [Fact]
    public async Task US239_NoRecoveryUpdate_WhenExerciseCatalogCannotBeResolved()
    {
        SetupSaveCapturesLog();
        var quest = BuildQuestWithCompletedExercise(UserId);
        var progression = HunterProgression.Create(UserId);

        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        // _exerciseCatalogRepository sem setup -> retorna null por padrão (loose mock).

        var act = () => CreateHandler().Handle(new CompleteQuestCommand(quest.Id), CancellationToken.None);

        await act.Should().NotThrowAsync();
        _muscleRecoveryStateRepository.Verify(r => r.AddAsync(It.IsAny<MuscleRecoveryState>(), It.IsAny<CancellationToken>()), Times.Never);
        _muscleRecoveryStateRepository.Verify(r => r.Update(It.IsAny<MuscleRecoveryState>()), Times.Never);
    }

    // US-241 §6.2: "como você se sentiu?" é persistido no QuestLog - único sinal
    // real de desempenho usado pela progressão semanal.
    [Fact]
    public async Task US241_PersistsPerceivedFeelingOnQuestLog()
    {
        SetupSaveCapturesLog();
        var quest = BuildQuestWithCompletedExercise(UserId);
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(HunterProgression.Create(UserId));

        await CreateHandler().Handle(
            new CompleteQuestCommand(quest.Id, PerceivedFeeling: "too_easy"), CancellationToken.None);

        _questLogRepository.Verify(r => r.AddAsync(
            It.Is<QuestLog>(log => log.PerceivedFeeling == "too_easy"), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Retrocompatibilidade: chamadas sem PerceivedFeeling (parâmetro opcional) continuam válidas.
    [Fact]
    public async Task US241_PerceivedFeelingDefaultsToNull_WhenNotProvided()
    {
        SetupSaveCapturesLog();
        var quest = BuildQuestWithCompletedExercise(UserId);
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _hunterProgressionRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(HunterProgression.Create(UserId));

        await CreateHandler().Handle(new CompleteQuestCommand(quest.Id), CancellationToken.None);

        _questLogRepository.Verify(r => r.AddAsync(
            It.Is<QuestLog>(log => log.PerceivedFeeling == null), It.IsAny<CancellationToken>()), Times.Once);
    }
}
