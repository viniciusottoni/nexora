using Awaken.Application.Admin.Media.Queries.GetMediaDiagnosticsList;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.Media;

/// <summary>
/// US-222 — testes do handler de listagem/filtro de diagnóstico de mídia do catálogo.
///
/// CA: exercício com imagem válida aparece como "ok".
/// CA: exercício sem nenhuma mídia aparece como "missing" (RN-001).
/// CA: exercício com URL inválida aparece como "invalid_link" (RN-002).
/// CA: asset lento aparece como "slow" (RN-003).
/// CA: filtro por status de mídia funciona.
/// CA: nenhuma credencial de storage é exposta na resposta (RN-005).
/// </summary>
public class GetMediaDiagnosticsListQueryHandlerTests
{
    private readonly Mock<IExerciseCatalogRepository> _repository = new();
    private readonly Mock<IMediaDiagnosticsService> _mediaDiagnosticsService = new();

    private GetMediaDiagnosticsListQueryHandler CreateHandler() =>
        new(_repository.Object, _mediaDiagnosticsService.Object);

    private static ExerciseCatalog BuildExercise(
        string providerExerciseId = "0001",
        string? imageUrl = "https://cdn.awaken.app/img.jpg",
        string? videoUrl = null,
        string? gifUrl = null,
        string environment = "home",
        string difficultyLevel = "beginner",
        string equipmentCategory = "bodyweight",
        string primaryRegion = "upper_body")
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
            EquipmentCategory: equipmentCategory,
            LoadType: "bodyweight",
            PrimaryRegion: primaryRegion,
            DifficultyLevel: difficultyLevel,
            DifficultyRank: 1,
            TechnicalComplexity: 1,
            ImpactLevel: 1,
            Environment: environment,
            RequiredEquipment: [],
            PrimaryMuscleGroups: ["chest"],
            SecondaryMuscleGroups: [],
            BodyParts: ["chest"],
            JointStressTags: [],
            ContraindicationTags: [],
            LimitationBlockTags: [],
            PainBlockTags: [],
            GoalTags: [],
            RiskTags: [],
            AccessibilityTags: [],
            TaxonomySignals: [],
            MinExperienceLevel: difficultyLevel,
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
            VideoUrl: videoUrl,
            ImageUrl: imageUrl,
            GifUrl: gifUrl,
            MediaLicenseInfo: null,
            SanitizationStatus: "approved",
            IsApprovedForWorkoutGeneration: true,
            Confidence: "high");

        return ExerciseCatalog.Create(snapshot, DateTime.UtcNow);
    }

    private void SetupDiagnosis(ExerciseCatalog exercise, MediaAssetDiagnostics result) =>
        _mediaDiagnosticsService
            .Setup(s => s.DiagnoseAsync(
                exercise.Id, exercise.ImageUrl, exercise.VideoUrl, exercise.GifUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    private static readonly MediaAssetDiagnostic Missing = new(MediaAssetStatus.Missing, null, null, null);

    [Fact]
    public async Task Handler_ExerciseWithValidImage_ReturnsOkStatus()
    {
        var exercise = BuildExercise();
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([exercise]);
        SetupDiagnosis(exercise, new MediaAssetDiagnostics(
            exercise.Id,
            Image: new MediaAssetDiagnostic(MediaAssetStatus.Valid, 200, 120, true),
            Video: Missing,
            Gif: Missing));

        var result = await CreateHandler().Handle(
            new GetMediaDiagnosticsListQuery(null, null, null, null, null, 1, 20), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().MediaStatus.Should().Be("ok");
        result.Items.Single().ImageStatus.Should().Be("valid");
    }

    [Fact]
    public async Task Handler_ExerciseWithoutMedia_ReturnsMissingStatus()
    {
        var exercise = BuildExercise(imageUrl: null);
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([exercise]);
        SetupDiagnosis(exercise, new MediaAssetDiagnostics(exercise.Id, Missing, Missing, Missing));

        var result = await CreateHandler().Handle(
            new GetMediaDiagnosticsListQuery(null, null, null, null, null, 1, 20), CancellationToken.None);

        result.Items.Single().MediaStatus.Should().Be("missing", "RN-001: sem mídia mínima nem fallback");
    }

    [Fact]
    public async Task Handler_ExerciseWithInvalidUrl_ReturnsInvalidLinkStatus()
    {
        var exercise = BuildExercise();
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([exercise]);
        SetupDiagnosis(exercise, new MediaAssetDiagnostics(
            exercise.Id,
            Image: new MediaAssetDiagnostic(MediaAssetStatus.Invalid, 404, 80, null),
            Video: Missing,
            Gif: Missing));

        var result = await CreateHandler().Handle(
            new GetMediaDiagnosticsListQuery(null, null, null, null, null, 1, 20), CancellationToken.None);

        result.Items.Single().MediaStatus.Should().Be("invalid_link", "RN-002: link inválido é problema operacional");
    }

    [Fact]
    public async Task Handler_SlowValidAsset_ReturnsSlowStatus()
    {
        var exercise = BuildExercise();
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([exercise]);
        SetupDiagnosis(exercise, new MediaAssetDiagnostics(
            exercise.Id,
            Image: new MediaAssetDiagnostic(MediaAssetStatus.Valid, 200, 3000, false),
            Video: Missing,
            Gif: Missing));

        var result = await CreateHandler().Handle(
            new GetMediaDiagnosticsListQuery(null, null, null, null, null, 1, 20), CancellationToken.None);

        result.Items.Single().MediaStatus.Should().Be("slow", "RN-003: asset pesado/lento deve aparecer como atenção");
    }

    [Fact]
    public async Task Handler_FilterByStatus_ReturnsOnlyMatchingExercises()
    {
        var okExercise = BuildExercise(providerExerciseId: "ok-1");
        var missingExercise = BuildExercise(providerExerciseId: "missing-1", imageUrl: null);

        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([okExercise, missingExercise]);

        SetupDiagnosis(okExercise, new MediaAssetDiagnostics(
            okExercise.Id, new MediaAssetDiagnostic(MediaAssetStatus.Valid, 200, 100, true), Missing, Missing));
        SetupDiagnosis(missingExercise, new MediaAssetDiagnostics(missingExercise.Id, Missing, Missing, Missing));

        var result = await CreateHandler().Handle(
            new GetMediaDiagnosticsListQuery(null, null, null, null, "missing", 1, 20), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().ExerciseId.Should().Be(missingExercise.Id);
    }

    [Fact]
    public async Task Handler_FilterByEnvironment_AppliesStructuralFilterBeforeDiagnosis()
    {
        var homeExercise = BuildExercise(providerExerciseId: "home-1", environment: "home");
        var gymExercise = BuildExercise(providerExerciseId: "gym-1", environment: "gym");

        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([homeExercise, gymExercise]);

        SetupDiagnosis(homeExercise, new MediaAssetDiagnostics(
            homeExercise.Id, new MediaAssetDiagnostic(MediaAssetStatus.Valid, 200, 100, true), Missing, Missing));

        var result = await CreateHandler().Handle(
            new GetMediaDiagnosticsListQuery("home", null, null, null, null, 1, 20), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().Environment.Should().Be("home");
        _mediaDiagnosticsService.Verify(
            s => s.DiagnoseAsync(gymExercise.Id, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handler_Response_NeverExposesStorageCredentials()
    {
        // RN-005: a resposta deve conter apenas campos do contrato — não há propriedade alguma
        // para chave/segredo de storage no DTO; este teste documenta a garantia via reflexão.
        var exercise = BuildExercise();
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([exercise]);
        SetupDiagnosis(exercise, new MediaAssetDiagnostics(
            exercise.Id, new MediaAssetDiagnostic(MediaAssetStatus.Valid, 200, 100, true), Missing, Missing));

        var result = await CreateHandler().Handle(
            new GetMediaDiagnosticsListQuery(null, null, null, null, null, 1, 20), CancellationToken.None);

        var properties = result.Items.Single().GetType().GetProperties().Select(p => p.Name);
        properties.Should().NotContain(p =>
            p.Contains("Key", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("AccountId", StringComparison.OrdinalIgnoreCase));
    }
}
