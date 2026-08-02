using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Services.Quests;
using FluentAssertions;

namespace Awaken.UnitTests.Quests;

public class ExerciseSelectionEngineTests
{
    private static ExerciseCatalog BuildExercise(
        string providerExerciseId,
        string movementPattern,
        List<string> primaryMuscleGroups,
        int difficultyRank = 1,
        bool isWeighted = false)
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
            PrimaryRegion: "upper_body",
            DifficultyLevel: "beginner",
            DifficultyRank: difficultyRank,
            TechnicalComplexity: 1,
            ImpactLevel: 1,
            Environment: "home",
            RequiredEquipment: [],
            PrimaryMuscleGroups: primaryMuscleGroups,
            SecondaryMuscleGroups: [],
            BodyParts: ["chest"],
            JointStressTags: [],
            ContraindicationTags: [],
            LimitationBlockTags: [],
            PainBlockTags: [],
            GoalTags: ["strength"],
            RiskTags: [],
            AccessibilityTags: [],
            TaxonomySignals: [],
            MinExperienceLevel: "beginner",
            SuitableForSedentary: true,
            SuitableForBeginner: true,
            SuitableForIntermediate: true,
            SuitableForAdvanced: true,
            IsCompound: false,
            IsUnilateral: false,
            IsAssisted: false,
            IsWeighted: isWeighted,
            RegressionExerciseIds: [],
            ProgressionExerciseIds: [],
            RelatedExerciseIds: [],
            VideoUrl: null,
            ImageUrl: null,
            GifUrl: null,
            MediaLicenseInfo: null,
            SanitizationStatus: "approved",
            IsApprovedForWorkoutGeneration: true,
            Confidence: "high");

        return ExerciseCatalog.Create(snapshot, DateTime.UtcNow);
    }

    private static ExerciseScore BuildScore(
        ExerciseCatalog exercise,
        double goalAffinityScore = 0.5,
        double safetyScore = 0.5,
        double levelMatchScore = 0.5,
        double timeFitScore = 0.5,
        double progressionFitScore = 0.5,
        double targetAttributeScore = 0.5,
        double varietyScore = 0.5) =>
        new(
            exercise,
            GoalAffinityScore: goalAffinityScore,
            SafetyScore: safetyScore,
            LevelMatchScore: levelMatchScore,
            TimeFitScore: timeFitScore,
            VarietyScore: varietyScore,
            ProgressionFitScore: progressionFitScore,
            TargetAttributeScore: targetAttributeScore);

    [Fact]
    public void Select_PrefersNovelPatternAndMuscleGroup_WhenScoresTie()
    {
        var first = BuildExercise("a", "push", ["chest"]);
        var novel = BuildExercise("b", "pull", ["back"]);

        var selected = ExerciseSelectionEngine.Select(
            [BuildScore(first), BuildScore(novel)],
            availableMinutesPerWorkout: 10);

        selected.Should().HaveCount(2);
        selected[0].Exercise.ProviderExerciseId.Should().Be("a");
        selected[1].Exercise.ProviderExerciseId.Should().Be("b");
    }

    // targetAttributeScore decide desempate entre candidatos com scores idênticos no resto.
    [Fact]
    public void Select_PrefersHigherTargetAttribute_WhenAllOtherScoresAreEqual()
    {
        var first = BuildExercise("a", "push", ["chest"]);
        var lowTarget = BuildExercise("b", "pull", ["back"]);
        var highTarget = BuildExercise("c", "pull", ["back"]);

        var selected = ExerciseSelectionEngine.Select(
            [
                // "a" has high safety — selected first.
                BuildScore(first, safetyScore: 1.0),
                // "b" e "c" iguais em tudo, só diferem em targetAttributeScore.
                BuildScore(lowTarget, targetAttributeScore: 0.25),
                BuildScore(highTarget, targetAttributeScore: 0.75),
            ],
            availableMinutesPerWorkout: 10);

        selected.Should().HaveCount(2);
        selected[0].Exercise.ProviderExerciseId.Should().Be("a");
        selected[1].Exercise.ProviderExerciseId.Should().Be("c");
    }
}
