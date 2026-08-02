using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Common;
using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Entities.Training;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Quests;

// US-240: DailyWorkoutBlueprintBuilder combina split (US-237) + rotação do dia
// (US-238) + plano de recuperação (US-239) + catálogo aprovado (US-035) num
// alvo determinístico do dia. Testes nomeados pelas CAs da US-240.
public class DailyWorkoutBlueprintBuilderTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 7, 5, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserWorkoutPreferenceRepository> _preferenceRepository = new();
    private readonly Mock<ITrainingProgramSplitRepository> _splitRepository = new();
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<IMuscleRecoveryStateRepository> _recoveryRepository = new();
    private readonly Mock<IExerciseCatalogRepository> _catalogRepository = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<ILogger<DailyWorkoutBlueprintBuilder>> _logger = new();

    public DailyWorkoutBlueprintBuilderTests()
    {
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
        _recoveryRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<MuscleRecoveryState>)[]);
    }

    private DailyWorkoutBlueprintBuilder CreateBuilder() => new(
        _preferenceRepository.Object,
        _splitRepository.Object,
        _questRepository.Object,
        _recoveryRepository.Object,
        _catalogRepository.Object,
        _dateTimeService.Object,
        _logger.Object);

    // abc — mesmos dados literais do seed real (Task 2.3/US-237), copiados aqui
    // para não depender de infraestrutura/migração num teste unitário puro.
    private static TrainingProgramSplit BuildAbcSplit() => TrainingProgramSplit.Create(
        TrainingProgramKeys.Abc, "v1",
        [
            new TrainingSplitDaySeed(
                "A", "programDayPush", "push",
                [MuscleGroups.Chest, MuscleGroups.Shoulders, MuscleGroups.Triceps], [],
                [MovementPatterns.HorizontalPush, MovementPatterns.VerticalPush, MovementPatterns.CoreFlexion],
                true, 4, 6),
            new TrainingSplitDaySeed(
                "B", "programDayPull", "pull",
                [MuscleGroups.Back, MuscleGroups.Biceps, MuscleGroups.RearDelts, MuscleGroups.Traps], [],
                [MovementPatterns.HorizontalPull, MovementPatterns.VerticalPull],
                false, 4, 6),
            new TrainingSplitDaySeed(
                "C", "programDayLegs", "legs",
                [MuscleGroups.Quadriceps, MuscleGroups.Hamstrings, MuscleGroups.Glutes, MuscleGroups.Calves, MuscleGroups.Core], [],
                [MovementPatterns.Squat, MovementPatterns.Hinge, MovementPatterns.Lunge, MovementPatterns.CoreFlexion],
                true, 4, 6),
        ]);

    private static Quest BuildLastCompletedQuest(string programKey, string dayKey, int dayIndex, string splitMapVersion)
    {
        var quest = Quest.Create(UserId, UtcNow.Date.AddDays(-1), "pt-BR", Guid.NewGuid().ToString());
        quest.AssignResolvedProgramDay(programKey, dayKey, dayIndex, splitMapVersion, UtcNow.AddDays(-1));
        return quest;
    }

    private static ExerciseCatalog BuildExercise(string movementPattern, params string[] primaryMuscleGroups)
    {
        var snapshot = new ExerciseCatalogSnapshot(
            RawImportId: null,
            ProviderName: "test",
            ProviderExerciseId: Guid.NewGuid().ToString(),
            ProviderVersion: null,
            NamePtBr: "Exercicio",
            NameOriginal: "Exercise",
            Slug: "exercise",
            DescriptionPtBr: "Descricao",
            InstructionsPtBr: ["Passo 1"],
            InstructionsOriginal: ["Step 1"],
            TipsPtBr: [],
            ExerciseType: "strength",
            MovementPattern: movementPattern,
            MovementFamily: "family",
            Mechanic: "compound",
            ForceType: "push",
            PlaneOfMotion: "sagittal",
            Laterality: "bilateral",
            BodyPosition: "standing",
            BenchAngle: null,
            EquipmentCategory: "bodyweight",
            LoadType: "bodyweight",
            PrimaryRegion: "lower_body",
            DifficultyLevel: "intermediate",
            DifficultyRank: 1,
            TechnicalComplexity: 1,
            ImpactLevel: 1,
            Environment: "home",
            RequiredEquipment: [],
            PrimaryMuscleGroups: primaryMuscleGroups.ToList(),
            SecondaryMuscleGroups: [],
            BodyParts: [],
            JointStressTags: [],
            ContraindicationTags: [],
            LimitationBlockTags: [],
            PainBlockTags: [],
            GoalTags: [],
            RiskTags: [],
            AccessibilityTags: [],
            TaxonomySignals: [],
            MinExperienceLevel: "sedentary",
            SuitableForSedentary: true,
            SuitableForBeginner: true,
            SuitableForIntermediate: true,
            SuitableForAdvanced: true,
            IsCompound: false,
            IsUnilateral: false,
            IsAssisted: false,
            IsWeighted: false,
            RegressionExerciseIds: [],
            ProgressionExerciseIds: [],
            RelatedExerciseIds: [],
            VideoUrl: "https://video.example/ex.mp4",
            ImageUrl: null,
            GifUrl: "https://gif.example/ex.gif",
            MediaLicenseInfo: null,
            SanitizationStatus: "approved",
            IsApprovedForWorkoutGeneration: true,
            Confidence: "high");

        return ExerciseCatalog.Create(snapshot, UtcNow);
    }

    private void SetupAbcOnDayC()
    {
        _preferenceRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserWorkoutPreference.Create(UserId, "program", TrainingProgramKeys.Abc, UtcNow));
        _splitRepository
            .Setup(r => r.GetByProgramKeyAsync(TrainingProgramKeys.Abc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildAbcSplit());
        // último concluído foi o dia B (pull) -> sucessor cíclico é o dia C (legs).
        _questRepository
            .Setup(r => r.GetLastCompletedByUserAndProgramAsync(UserId, TrainingProgramKeys.Abc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildLastCompletedQuest(TrainingProgramKeys.Abc, "B", 2, "v1"));
    }

    [Fact]
    public async Task CA001_DayCOfAbc_TargetsLegsPatternsAndCountsOnlyCoherentEligibles()
    {
        SetupAbcOnDayC();
        _catalogRepository
            .Setup(r => r.ListApprovedForWorkoutGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ExerciseCatalog>)
            [
                BuildExercise(MovementPatterns.Squat, MuscleGroups.Quadriceps),
                BuildExercise(MovementPatterns.Hinge, MuscleGroups.Hamstrings),
                BuildExercise(MovementPatterns.Lunge, MuscleGroups.Glutes),
                BuildExercise(MovementPatterns.CoreFlexion, MuscleGroups.Core),
                // Incoerentes com o dia (peito/push) - não devem contar.
                BuildExercise(MovementPatterns.HorizontalPush, MuscleGroups.Chest),
                BuildExercise(MovementPatterns.VerticalPush, MuscleGroups.Shoulders),
            ]);

        var result = await CreateBuilder().BuildAsync(UserId, "intermediate", CancellationToken.None);

        result.ProgramKey.Should().Be(TrainingProgramKeys.Abc);
        result.ResolvedDayKey.Should().Be("C");
        result.TargetPatterns.Should().BeEquivalentTo(
            [MovementPatterns.Squat, MovementPatterns.Hinge, MovementPatterns.Lunge, MovementPatterns.CoreFlexion]);
        result.EligibleExerciseCount.Should().Be(4);
        result.FallbackUsed.Should().BeFalse();
    }

    [Fact]
    public async Task CA002_RecentHeavySessionOnQuadriceps_ReducesVolumeBudgetAndRpeMaxBelowDefault()
    {
        SetupAbcOnDayC();
        _catalogRepository
            .Setup(r => r.ListApprovedForWorkoutGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ExerciseCatalog>)
            [
                BuildExercise(MovementPatterns.Squat, MuscleGroups.Quadriceps),
                BuildExercise(MovementPatterns.Hinge, MuscleGroups.Hamstrings),
                BuildExercise(MovementPatterns.Lunge, MuscleGroups.Glutes),
                BuildExercise(MovementPatterns.CoreFlexion, MuscleGroups.Core),
            ]);
        // Quadriceps treinado pesado há 1h (< 50% da janela de 72h) => Fatigued (RN-003 US-239).
        var recoveryState = MuscleRecoveryState.CreateInitial(UserId, MuscleGroups.Quadriceps, UtcNow.AddHours(-1));
        recoveryState.RegisterSession(UtcNow.AddHours(-1), "heavy", ["back_squat"], setsPerformed: 12);
        _recoveryRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<MuscleRecoveryState>)[recoveryState]);

        var result = await CreateBuilder().BuildAsync(UserId, "intermediate", CancellationToken.None);

        var recovered = result.TargetGroups.Single(g => g.MuscleGroup == MuscleGroups.Hamstrings);
        var quadriceps = result.TargetGroups.Single(g => g.MuscleGroup == MuscleGroups.Quadriceps);

        quadriceps.IsInRecovery.Should().BeTrue();
        quadriceps.VolumeBudgetSets.Should().BeLessThan(recovered.VolumeBudgetSets);
        quadriceps.RpeMax.Should().BeLessThan(recovered.RpeMax);
        quadriceps.AvoidMovementFamilies.Should().Contain("back_squat");
    }

    [Fact]
    public async Task CA003_EligibleCountBelowDayMinimum_TriggersFallback()
    {
        SetupAbcOnDayC();
        // Dia C exige mínimo de 4 exercícios (seed real); só 1 elegível coerente.
        _catalogRepository
            .Setup(r => r.ListApprovedForWorkoutGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ExerciseCatalog>)
            [
                BuildExercise(MovementPatterns.Squat, MuscleGroups.Quadriceps),
            ]);

        var result = await CreateBuilder().BuildAsync(UserId, "intermediate", CancellationToken.None);

        result.EligibleExerciseCount.Should().Be(1);
        result.FallbackUsed.Should().BeTrue();
    }

    [Fact]
    public async Task CA004_NoPreferenceSelected_DefaultsToFullBody()
    {
        _preferenceRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserWorkoutPreference?)null);
        _splitRepository
            .Setup(r => r.GetByProgramKeyAsync(TrainingProgramKeys.FullBody, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrainingProgramSplit?)null);

        var result = await CreateBuilder().BuildAsync(UserId, "sedentary", CancellationToken.None);

        result.ProgramKey.Should().Be(TrainingProgramKeys.FullBody);
        // RN-009: programa sem split classico configurado -> fallback trivial (nao incoerente).
        result.FallbackUsed.Should().BeTrue();
    }

    [Fact]
    public async Task Determinism_SameInputTwice_ProducesEquivalentBlueprint()
    {
        SetupAbcOnDayC();
        _catalogRepository
            .Setup(r => r.ListApprovedForWorkoutGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ExerciseCatalog>)
            [
                BuildExercise(MovementPatterns.Squat, MuscleGroups.Quadriceps),
                BuildExercise(MovementPatterns.Hinge, MuscleGroups.Hamstrings),
                BuildExercise(MovementPatterns.Lunge, MuscleGroups.Glutes),
                BuildExercise(MovementPatterns.CoreFlexion, MuscleGroups.Core),
            ]);

        var builder = CreateBuilder();
        var first = await builder.BuildAsync(UserId, "intermediate", CancellationToken.None);
        var second = await builder.BuildAsync(UserId, "intermediate", CancellationToken.None);

        first.Should().BeEquivalentTo(second);
    }
}
