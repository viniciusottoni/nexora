using Awaken.Application.Common.Exceptions;
using Awaken.Application.Exercises.Queries.GetExerciseRelationships;
using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Exercises;

/// <summary>
/// US-236 — leitura de candidatos de relação (similares/substitutos/progressões/regressões) ordenados
/// por score, para consumo do motor de geração/troca de exercício (EPIC-006/007).
/// </summary>
public class GetExerciseRelationshipsQueryHandlerTests
{
    private readonly Mock<IExerciseCatalogRepository> _repository = new();
    private static readonly DateTime UtcNow = new(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc);

    private GetExerciseRelationshipsQueryHandler CreateHandler() => new(_repository.Object);

    private static ExerciseCatalog BuildCatalogWithRelations(params ExerciseRelationship[] relations)
    {
        var snapshot = new ExerciseCatalogSnapshot(
            RawImportId: null,
            ProviderName: "local_files",
            ProviderExerciseId: "0025",
            ProviderVersion: null,
            NamePtBr: "Supino reto",
            NameOriginal: "Barbell bench press",
            Slug: "supino-reto",
            DescriptionPtBr: null,
            InstructionsPtBr: ["Passo 1"],
            InstructionsOriginal: ["Step 1"],
            TipsPtBr: [],
            ExerciseType: "strength",
            MovementPattern: "horizontal_push",
            MovementFamily: "bench_press",
            Mechanic: "compound",
            ForceType: "push",
            PlaneOfMotion: "sagittal",
            Laterality: "bilateral",
            BodyPosition: "lying",
            BenchAngle: "flat",
            EquipmentCategory: "free_weight",
            LoadType: "free_weight",
            PrimaryRegion: "upper_body",
            DifficultyLevel: "intermediate",
            DifficultyRank: 3,
            TechnicalComplexity: 3,
            ImpactLevel: 2,
            Environment: "gym",
            RequiredEquipment: ["barbell"],
            PrimaryMuscleGroups: ["pectorals"],
            SecondaryMuscleGroups: ["triceps"],
            BodyParts: ["chest"],
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
            GifUrl: "https://cdn.awaken.app/0025/360.gif",
            MediaLicenseInfo: null,
            SanitizationStatus: "pending_review",
            IsApprovedForWorkoutGeneration: false,
            Confidence: "high");

        var catalog = ExerciseCatalog.Create(snapshot, UtcNow);
        catalog.ReplaceRelations(relations, UtcNow);
        return catalog;
    }

    [Fact]
    public async Task HandleReturnsAllRelationsOrderedByScoreDescendingWhenNoCategoryIsGiven()
    {
        var high = ExerciseRelationship.Create("0289", "Dumbbell bench press", "substitution", ["equipment_alternative"], 90m, "high", ["reason a"]);
        var mid = ExerciseRelationship.Create("0033", "Decline bench press", "similar", [], 70m, "medium", ["reason b"]);
        var low = ExerciseRelationship.Create("0748", "Smith bench press", "regression", ["lower_difficulty"], 50m, "low", ["reason c"]);
        var catalog = BuildCatalogWithRelations(low, high, mid);
        _repository.Setup(r => r.GetByProviderExerciseIdAsync("local_files", "0025", It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalog);

        var result = await CreateHandler().Handle(
            new GetExerciseRelationshipsQuery("local_files", "0025", null), CancellationToken.None);

        result.Should().HaveCount(3);
        result.Select(r => r.Score).Should().ContainInOrder(90m, 70m, 50m);
    }

    [Fact]
    public async Task HandleFiltersByCategoryWhenGiven()
    {
        var substitution = ExerciseRelationship.Create("0289", "Dumbbell bench press", "substitution", ["equipment_alternative"], 90m, "high", []);
        var progression = ExerciseRelationship.Create("0045", "Guillotine bench press", "progression", ["higher_difficulty"], 95m, "high", []);
        var catalog = BuildCatalogWithRelations(substitution, progression);
        _repository.Setup(r => r.GetByProviderExerciseIdAsync("local_files", "0025", It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalog);

        var result = await CreateHandler().Handle(
            new GetExerciseRelationshipsQuery("local_files", "0025", "progression"), CancellationToken.None);

        result.Should().ContainSingle();
        result.Single().RelationCategory.Should().Be("progression");
        result.Single().ExerciseId.Should().Be("0045");
    }

    [Fact]
    public async Task HandleThrowsNotFoundExceptionWhenProviderExerciseIdDoesNotExist()
    {
        _repository.Setup(r => r.GetByProviderExerciseIdAsync("local_files", "9999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExerciseCatalog?)null);

        var act = async () => await CreateHandler().Handle(
            new GetExerciseRelationshipsQuery("local_files", "9999", null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
