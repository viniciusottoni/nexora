using System.Text.Json;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Progression.Common;
using Awaken.Application.Quests.Common;
using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Training;
using Awaken.Domain.Repositories;
using Awaken.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Infrastructure;

public class WorkoutGeneratorServiceTests
{
    private readonly Mock<ILogger<WorkoutGeneratorService>> _logger = new();
    private readonly Mock<IExerciseCatalogRepository> _exerciseCatalogRepository = new();

    // US-240: DailyWorkoutBlueprintBuilder é uma classe concreta (não interface,
    // ver seu XML doc) - mesma convenção de QuestRegenerationService, testada com
    // uma instância real wireada sobre repositórios mockados, em vez de mockada
    // diretamente. Sem setup, os mocks retornam null/vazio (loose mocks), levando
    // ao caminho trivial de fallback (RN-009) e preservando o comportamento
    // anterior a esta US para os testes que não mexem com split/blueprint.
    private readonly Mock<IUserWorkoutPreferenceRepository> _preferenceRepository = new();
    private readonly Mock<ITrainingProgramSplitRepository> _splitRepository = new();
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<IMuscleRecoveryStateRepository> _recoveryRepository = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<ILogger<DailyWorkoutBlueprintBuilder>> _blueprintBuilderLogger = new();

    // US-241: WeeklyProgressionReviewer é classe concreta (mesmo precedente de
    // DailyWorkoutBlueprintBuilder acima) — nenhum teste desta classe passa
    // userProfile, então ReviewAsync nunca é chamado (guarda em GenerateWorkoutJsonAsync).
    private readonly Mock<IWeeklyProgressionStateRepository> _weeklyProgressionStateRepository = new();
    private readonly Mock<IQuestLogRepository> _questLogRepository = new();
    private readonly Mock<ILogger<WeeklyProgressionReviewer>> _reviewerLogger = new();

    public WorkoutGeneratorServiceTests()
    {
        _recoveryRepository
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<MuscleRecoveryState>)[]);
        _dateTimeService.Setup(s => s.UtcNow).Returns(DateTime.UtcNow);
    }

    private WorkoutGeneratorService CreateService()
    {
        var blueprintBuilder = new DailyWorkoutBlueprintBuilder(
            _preferenceRepository.Object,
            _splitRepository.Object,
            _questRepository.Object,
            _recoveryRepository.Object,
            _exerciseCatalogRepository.Object,
            _dateTimeService.Object,
            _blueprintBuilderLogger.Object);

        var weeklyProgressionReviewer = new WeeklyProgressionReviewer(
            _weeklyProgressionStateRepository.Object,
            _questLogRepository.Object,
            _dateTimeService.Object,
            _reviewerLogger.Object);

        return new WorkoutGeneratorService(
            _logger.Object,
            _exerciseCatalogRepository.Object,
            blueprintBuilder,
            weeklyProgressionReviewer);
    }

    private static ExerciseCatalog BuildApprovedExercise(
        string minExperienceLevel = "intermediate",
        List<string>? limitationBlockTags = null)
    {
        var snapshot = new ExerciseCatalogSnapshot(
            RawImportId: null,
            ProviderName: "test",
            ProviderExerciseId: "exercise-1",
            ProviderVersion: null,
            NamePtBr: "Exercicio 1",
            NameOriginal: "Exercise 1",
            Slug: "exercise-1",
            DescriptionPtBr: "Descricao",
            InstructionsPtBr: ["Passo 1"],
            InstructionsOriginal: ["Step 1"],
            TipsPtBr: [],
            ExerciseType: "strength",
            MovementPattern: "push",
            MovementFamily: "press",
            Mechanic: "compound",
            ForceType: "push",
            PlaneOfMotion: "sagittal",
            Laterality: "bilateral",
            BodyPosition: "standing",
            BenchAngle: null,
            EquipmentCategory: "bodyweight",
            LoadType: "bodyweight",
            PrimaryRegion: "upper_body",
            DifficultyLevel: "intermediate",
            DifficultyRank: 1,
            TechnicalComplexity: 1,
            ImpactLevel: 1,
            Environment: "home",
            RequiredEquipment: [],
            PrimaryMuscleGroups: ["chest"],
            SecondaryMuscleGroups: [],
            BodyParts: ["chest"],
            JointStressTags: [],
            ContraindicationTags: [],
            LimitationBlockTags: limitationBlockTags ?? [],
            PainBlockTags: [],
            GoalTags: [],
            RiskTags: [],
            AccessibilityTags: [],
            TaxonomySignals: [],
            MinExperienceLevel: minExperienceLevel,
            SuitableForSedentary: true,
            SuitableForBeginner: true,
            SuitableForIntermediate: true,
            SuitableForAdvanced: false,
            IsCompound: false,
            IsUnilateral: false,
            IsAssisted: false,
            IsWeighted: false,
            RegressionExerciseIds: [],
            ProgressionExerciseIds: [],
            RelatedExerciseIds: [],
            VideoUrl: "https://video.example/ex.mp4",
            ImageUrl: null,
            GifUrl: null,
            MediaLicenseInfo: null,
            SanitizationStatus: "approved",
            IsApprovedForWorkoutGeneration: true,
            Confidence: "high");

        return ExerciseCatalog.Create(snapshot, DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateWorkoutJsonAsync_UsesTrainingDurationWhenEffectiveLevelIsMissing()
    {
        _exerciseCatalogRepository.Setup(r => r.ListApprovedForWorkoutGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([BuildApprovedExercise()]);

        var service = CreateService();

        var result = await service.GenerateWorkoutJsonAsync(
            Guid.NewGuid(),
            "pt-BR",
            """
            {
              "goal": "gain_muscle",
              "experienceLevel": "advanced",
              "trainingDuration": "does_not_train",
              "availableMinutesPerWorkout": 30,
              "equipmentAvailable": [],
              "physicalLimitations": [],
              "physicalPains": [],
              "heightCm": 175,
              "weightKg": 82
            }
            """);

        result.IsPersonalized.Should().BeFalse();
        result.GenerationMethod.Should().Be("fallback_template");

        using var document = JsonDocument.Parse(result.AppliedFiltersJson);
        document.RootElement.GetProperty("effectiveExperienceLevel").GetString().Should().Be("sedentary");
    }

    [Fact]
    public async Task SelectSubstituteExerciseAsync_TranslatesOnboardingLimitationTagBeforeSafetyFilter()
    {
        // US-040: "knee_problem" e vocabulario de onboarding (CompleteOnboardingCommandValidator);
        // "knee_high_stress" e vocabulario do catalogo (ImportExercisesCommandHandler.BuildJointStressTags).
        // Sem a traducao em ParseProfile, ExerciseSafetyFilter.HasConflict compara string exata entre os
        // dois e nunca bate - o exercicio abaixo (sem regressao) permaneceria elegivel indevidamente.
        _exerciseCatalogRepository.Setup(r => r.ListApprovedForWorkoutGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([BuildApprovedExercise(limitationBlockTags: ["knee_high_stress"])]);

        var service = CreateService();

        var result = await service.SelectSubstituteExerciseAsync(
            """
            {
              "goal": "gain_muscle",
              "effectiveExperienceLevel": "intermediate",
              "availableMinutesPerWorkout": 30,
              "equipmentAvailable": [],
              "physicalLimitations": ["knee_problem"],
              "physicalPains": [],
              "heightCm": 175,
              "weightKg": 82
            }
            """,
            excludeProviderExerciseIds: []);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GenerateWorkoutJsonAsync_IncludesTimeBudgetFieldsInWorkoutJson()
    {
        // US-242: exercicio elegivel (nivel bate, sem limitacoes) -> alcanca o caminho
        // personalizado ("catalog_rules"), onde o TimeBudgetCalculator roda de fato.
        _exerciseCatalogRepository.Setup(r => r.ListApprovedForWorkoutGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([BuildApprovedExercise(minExperienceLevel: "beginner")]);

        var service = CreateService();

        var result = await service.GenerateWorkoutJsonAsync(
            Guid.NewGuid(),
            "pt-BR",
            """
            {
              "goal": "gain_muscle",
              "effectiveExperienceLevel": "beginner",
              "availableMinutesPerWorkout": 30,
              "equipmentAvailable": [],
              "physicalLimitations": [],
              "physicalPains": [],
              "heightCm": 175,
              "weightKg": 82
            }
            """);

        result.IsPersonalized.Should().BeTrue();
        result.EstimatedDurationSeconds.Should().NotBeNull();
        result.TimeBudgetSeconds.Should().Be(30 * 60);
        result.TimeAdjustmentApplied.Should().NotBeNullOrEmpty();
        result.WorkoutTimeModelVersion.Should().Be(Awaken.Domain.Services.Quests.WorkoutTimeModel.Version);

        using var document = JsonDocument.Parse(result.WorkoutJson);
        document.RootElement.GetProperty("estimatedDurationSeconds").GetInt32().Should().BeGreaterThan(0);
        document.RootElement.GetProperty("timeBudgetSeconds").GetInt32().Should().Be(30 * 60);
        document.RootElement.GetProperty("timeAdjustmentApplied").GetString().Should().NotBeNullOrEmpty();
        document.RootElement.GetProperty("workoutTimeModelVersion").GetString().Should().Be(Awaken.Domain.Services.Quests.WorkoutTimeModel.Version);
    }

    [Fact]
    public async Task GenerateWorkoutJsonAsync_NeverExceedsAvailableMinutes()
    {
        _exerciseCatalogRepository.Setup(r => r.ListApprovedForWorkoutGenerationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([BuildApprovedExercise(minExperienceLevel: "beginner")]);

        var service = CreateService();

        var result = await service.GenerateWorkoutJsonAsync(
            Guid.NewGuid(),
            "pt-BR",
            """
            {
              "goal": "gain_muscle",
              "effectiveExperienceLevel": "beginner",
              "availableMinutesPerWorkout": 12,
              "equipmentAvailable": [],
              "physicalLimitations": [],
              "physicalPains": [],
              "heightCm": 175,
              "weightKg": 82
            }
            """);

        result.EstimatedDurationSeconds.Should().NotBeNull();
        result.EstimatedDurationSeconds!.Value.Should().BeLessThanOrEqualTo(12 * 60);
    }
}
