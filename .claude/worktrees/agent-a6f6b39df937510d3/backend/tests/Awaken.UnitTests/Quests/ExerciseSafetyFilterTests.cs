using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Services.Quests;
using FluentAssertions;

namespace Awaken.UnitTests.Quests;

public class ExerciseSafetyFilterTests
{
    private static ExerciseCatalog BuildExercise(
        string providerExerciseId = "0001",
        string minExperienceLevel = "beginner",
        List<string>? requiredEquipment = null,
        int difficultyRank = 1,
        bool isWeighted = false,
        List<string>? limitationBlockTags = null,
        List<string>? painBlockTags = null,
        List<string>? contraindicationTags = null,
        int impactLevel = 1,
        int technicalComplexity = 1,
        bool isApproved = true,
        List<string>? regressionExerciseIds = null,
        List<string>? goalTags = null)
    {
        var snapshot = new ExerciseCatalogSnapshot(
            RawImportId: null,
            ProviderName: "test",
            ProviderExerciseId: providerExerciseId,
            ProviderVersion: null,
            NamePtBr: $"Exercicio {providerExerciseId}",
            NameOriginal: $"Exercise {providerExerciseId}",
            Slug: $"exercicio-{providerExerciseId}",
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
            DifficultyLevel: minExperienceLevel,
            DifficultyRank: difficultyRank,
            TechnicalComplexity: technicalComplexity,
            ImpactLevel: impactLevel,
            Environment: "home",
            RequiredEquipment: requiredEquipment ?? [],
            PrimaryMuscleGroups: ["chest"],
            SecondaryMuscleGroups: [],
            BodyParts: ["chest"],
            JointStressTags: [],
            ContraindicationTags: contraindicationTags ?? [],
            LimitationBlockTags: limitationBlockTags ?? [],
            PainBlockTags: painBlockTags ?? [],
            GoalTags: goalTags ?? [],
            RiskTags: [],
            AccessibilityTags: [],
            TaxonomySignals: [],
            MinExperienceLevel: minExperienceLevel,
            SuitableForSedentary: true,
            SuitableForBeginner: true,
            SuitableForIntermediate: true,
            SuitableForAdvanced: true,
            IsCompound: false,
            IsUnilateral: false,
            IsAssisted: false,
            IsWeighted: isWeighted,
            RegressionExerciseIds: regressionExerciseIds ?? [],
            ProgressionExerciseIds: [],
            RelatedExerciseIds: [],
            VideoUrl: "https://video.example/ex.mp4",
            ImageUrl: null,
            GifUrl: null,
            MediaLicenseInfo: null,
            SanitizationStatus: isApproved ? "approved" : "pending_review",
            IsApprovedForWorkoutGeneration: isApproved,
            Confidence: "high");

        return ExerciseCatalog.Create(snapshot, DateTime.UtcNow);
    }

    private static ExerciseSafetyContext BuildContext(
        string effectiveExperienceLevel = "intermediate",
        List<string>? equipmentAvailable = null,
        int availableMinutesPerWorkout = 30,
        List<string>? physicalLimitations = null,
        List<string>? physicalPains = null,
        decimal? bmi = null) =>
        new(
            effectiveExperienceLevel,
            equipmentAvailable ?? [],
            availableMinutesPerWorkout,
            physicalLimitations ?? [],
            physicalPains ?? [],
            bmi);

    [Fact]
    public void RN003_NeverIncludesExercisesNotApprovedForWorkoutGeneration()
    {
        var exercises = new[] { BuildExercise(isApproved: false) };

        var eligible = ExerciseSafetyFilter.Apply(exercises, BuildContext());

        eligible.Should().BeEmpty();
    }

    [Fact]
    public void RemovesExercise_WhenMinExperienceLevelAboveEffectiveLevel()
    {
        var exercises = new[] { BuildExercise(minExperienceLevel: "advanced") };

        var eligible = ExerciseSafetyFilter.Apply(exercises, BuildContext(effectiveExperienceLevel: "beginner"));

        eligible.Should().BeEmpty();
    }

    [Fact]
    public void KeepsExercise_WhenMinExperienceLevelAtOrBelowEffectiveLevel()
    {
        var exercises = new[] { BuildExercise(minExperienceLevel: "beginner") };

        var eligible = ExerciseSafetyFilter.Apply(exercises, BuildContext(effectiveExperienceLevel: "intermediate"));

        eligible.Should().ContainSingle(e => e.ProviderExerciseId == "0001");
    }

    [Fact]
    public void RemovesExercise_WhenRequiredEquipmentIsNotAvailable()
    {
        var exercises = new[] { BuildExercise(requiredEquipment: ["barbell"]) };

        var eligible = ExerciseSafetyFilter.Apply(exercises, BuildContext(equipmentAvailable: []));

        eligible.Should().BeEmpty();
    }

    [Fact]
    public void KeepsExercise_WhenRequiredEquipmentIsAvailable()
    {
        var exercises = new[] { BuildExercise(requiredEquipment: ["barbell"]) };

        var eligible = ExerciseSafetyFilter.Apply(exercises, BuildContext(equipmentAvailable: ["barbell"]));

        eligible.Should().ContainSingle();
    }

    [Fact]
    public void RemovesExercise_WhenEstimatedTimeCostExceedsAvailableMinutes()
    {
        // difficultyRank >= 3 + isWeighted => 4 sets * (10 reps * 3s + 90s rest) = 480s = 8 min.
        var exercises = new[] { BuildExercise(difficultyRank: 3, isWeighted: true) };

        var eligible = ExerciseSafetyFilter.Apply(exercises, BuildContext(availableMinutesPerWorkout: 5));

        eligible.Should().BeEmpty();
    }

    [Fact]
    public void CA001_RemovesExercise_WhenConflictsWithPhysicalLimitation()
    {
        var exercises = new[] { BuildExercise(limitationBlockTags: ["knee_high_stress"]) };

        var eligible = ExerciseSafetyFilter.Apply(
            exercises, BuildContext(physicalLimitations: ["knee_problem", "knee_high_stress"]));

        eligible.Should().BeEmpty();
    }

    [Fact]
    public void CA001_SubstitutesByRegression_WhenLimitationConflictsAndRegressionIsEligible()
    {
        var regression = BuildExercise(providerExerciseId: "0002", limitationBlockTags: []);
        var exercise = BuildExercise(
            providerExerciseId: "0001",
            limitationBlockTags: ["knee_high_stress"],
            regressionExerciseIds: ["0002"]);

        var eligible = ExerciseSafetyFilter.Apply(
            [exercise, regression], BuildContext(physicalLimitations: ["knee_high_stress"]));

        eligible.Should().ContainSingle(e => e.ProviderExerciseId == "0002");
    }

    [Fact]
    public void CA002_RemovesExercise_WhenConflictsWithPhysicalPain()
    {
        var exercises = new[] { BuildExercise(painBlockTags: ["lumbar_high_stress"]) };

        var eligible = ExerciseSafetyFilter.Apply(
            exercises, BuildContext(physicalPains: ["lower_back", "lumbar_high_stress"]));

        eligible.Should().BeEmpty();
    }

    [Fact]
    public void CA002_SubstitutesByRegression_WhenPainConflictsAndRegressionIsEligible()
    {
        var regression = BuildExercise(providerExerciseId: "0002", painBlockTags: []);
        var exercise = BuildExercise(
            providerExerciseId: "0001",
            painBlockTags: ["lumbar_high_stress"],
            regressionExerciseIds: ["0002"]);

        var eligible = ExerciseSafetyFilter.Apply(
            [exercise, regression], BuildContext(physicalPains: ["lumbar_high_stress"]));

        eligible.Should().ContainSingle(e => e.ProviderExerciseId == "0002");
    }

    [Fact]
    public void RemovesExercise_WhenHighImpactForSedentaryWithHighBmi()
    {
        var exercises = new[] { BuildExercise(impactLevel: 4) };

        var eligible = ExerciseSafetyFilter.Apply(
            exercises, BuildContext(effectiveExperienceLevel: "sedentary", bmi: 28));

        eligible.Should().BeEmpty();
    }

    [Fact]
    public void KeepsHighImpactExercise_WhenUserIsNotSedentary()
    {
        var exercises = new[] { BuildExercise(impactLevel: 4) };

        var eligible = ExerciseSafetyFilter.Apply(
            exercises, BuildContext(effectiveExperienceLevel: "intermediate", bmi: 28));

        eligible.Should().ContainSingle();
    }

    [Fact]
    public void RemovesExercise_WhenHighTechnicalComplexityForBeginner()
    {
        var exercises = new[] { BuildExercise(technicalComplexity: 4) };

        var eligible = ExerciseSafetyFilter.Apply(exercises, BuildContext(effectiveExperienceLevel: "beginner"));

        eligible.Should().BeEmpty();
    }

    [Fact]
    public void KeepsHighTechnicalComplexityExercise_WhenUserIsIntermediate()
    {
        var exercises = new[] { BuildExercise(technicalComplexity: 4) };

        var eligible = ExerciseSafetyFilter.Apply(exercises, BuildContext(effectiveExperienceLevel: "intermediate"));

        eligible.Should().ContainSingle();
    }

    [Fact]
    public void ReturnsEmptyList_WhenNoExerciseSurvivesTheFilter()
    {
        var exercises = new[]
        {
            BuildExercise(minExperienceLevel: "advanced"),
            BuildExercise(providerExerciseId: "0002", requiredEquipment: ["barbell"])
        };

        var eligible = ExerciseSafetyFilter.Apply(
            exercises, BuildContext(effectiveExperienceLevel: "sedentary", equipmentAvailable: []));

        eligible.Should().BeEmpty();
    }

    [Fact]
    public void RemovesExercise_WithoutSubstitution_WhenNoEligibleRegressionExists()
    {
        var blockedRegression = BuildExercise(providerExerciseId: "0002", limitationBlockTags: ["knee_high_stress"]);
        var exercise = BuildExercise(
            providerExerciseId: "0001",
            limitationBlockTags: ["knee_high_stress"],
            regressionExerciseIds: ["0002"]);

        var eligible = ExerciseSafetyFilter.Apply(
            [exercise, blockedRegression], BuildContext(physicalLimitations: ["knee_high_stress"]));

        eligible.Should().BeEmpty();
    }
}
