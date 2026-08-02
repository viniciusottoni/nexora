using Awaken.Application.Common.Interfaces;
using Awaken.Application.Exercises.Commands.ImportExercises;
using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Awaken.UnitTests.Exercises;

public class ImportExercisesCommandHandlerTests : IDisposable
{
    private readonly Mock<IExerciseRawImportRepository> _rawImportRepository = new();
    private readonly Mock<IExerciseCatalogRepository> _catalogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<ISafeDirectoryResolver> _safeDirectoryResolver = new();
    private readonly Mock<IExerciseCatalogCacheService> _exerciseCatalogCache = new();
    private readonly Mock<IMediaStorageService> _mediaStorageService = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<ILogger<ImportExercisesCommandHandler>> _logger = new();
    private readonly string _sourceDirectory;
    private const string BatchKey = "batch-2026-01";

    private static readonly DateTime UtcNow = new(2026, 6, 22, 10, 0, 0, DateTimeKind.Utc);

    public ImportExercisesCommandHandlerTests()
    {
        _sourceDirectory = Path.Combine(Path.GetTempPath(), $"awaken-exercises-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_sourceDirectory);
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
        _safeDirectoryResolver
            .Setup(r => r.Resolve(BatchKey))
            .Returns(_sourceDirectory);
        _rawImportRepository.Setup(r => r.GetByProviderExerciseIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExerciseRawImport?)null);
        _rawImportRepository.Setup(r => r.ExistsByProviderExerciseIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _catalogRepository.Setup(r => r.GetByProviderExerciseIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExerciseCatalog?)null);

        // US-236: por padrao, o upload de midia (mockado) sempre tem sucesso e devolve uma URL
        // publica deterministica derivada da key, para os testes que nao exercitam a falha de upload.
        _mediaStorageService
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, Stream _, string _, CancellationToken _) => $"https://cdn.test/api/media/{key}");
        _configuration.Setup(c => c["ExerciseImport:SignedEulaPath"]).Returns((string?)null);
    }

    public void Dispose()
    {
        if (Directory.Exists(_sourceDirectory))
            Directory.Delete(_sourceDirectory, recursive: true);
    }

    private ImportExercisesCommandHandler CreateHandler() => new(
        _rawImportRepository.Object,
        _catalogRepository.Object,
        _unitOfWork.Object,
        _dateTimeService.Object,
        _safeDirectoryResolver.Object,
        _exerciseCatalogCache.Object,
        _mediaStorageService.Object,
        _configuration.Object,
        _logger.Object);

    [Fact]
    public async Task HandleImportsRawJsonAndCreatesCatalogFromLocalFiles()
    {
        WriteSampleExercise("0025");

        _rawImportRepository.Setup(r => r.GetByProviderExerciseIdAsync(
                "local_files", "0025", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExerciseRawImport?)null);
        _catalogRepository.Setup(r => r.GetByProviderExerciseIdAsync(
                "local_files", "0025", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExerciseCatalog?)null);

        var result = await CreateHandler().Handle(
            new ImportExercisesCommand(BatchKey, "local_files", null, ApproveOnImport: false),
            CancellationToken.None);

        result.RawImported.Should().Be(1);
        result.CatalogCreated.Should().Be(1);
        result.CatalogUpdated.Should().Be(0);
        result.Failed.Should().Be(0);
        result.MediaUploaded.Should().Be(1);

        _rawImportRepository.Verify(r => r.AddAsync(It.Is<ExerciseRawImport>(raw =>
            raw.ProviderName == "local_files" &&
            raw.ProviderExerciseId == "0025" &&
            raw.RawJson.Contains("\"barbell bench press\"") &&
            raw.SourceFilePath == $"{BatchKey}/0025.json" &&
            raw.MediaBaseUrl == "https://cdn.test/api/media/exercises/0025/360.gif" &&
            raw.Status == ExerciseRawImportStatus.PendingReview), It.IsAny<CancellationToken>()), Times.Once);

        _catalogRepository.Verify(r => r.AddAsync(It.Is<ExerciseCatalog>(catalog =>
            catalog.ProviderExerciseId == "0025" &&
            catalog.NameOriginal == "barbell bench press" &&
            catalog.MovementPattern == "horizontal_push" &&
            catalog.RequiredEquipment.Contains("barbell") &&
            catalog.PrimaryMuscleGroups.Contains("pectorals") &&
            catalog.SecondaryMuscleGroups.Contains("triceps") &&
            catalog.GifUrl == "https://cdn.test/api/media/exercises/0025/360.gif" &&
            catalog.IsApprovedForWorkoutGeneration == false &&
            catalog.AttributeContribution != null &&
            catalog.AttributeContribution.PrimaryAttribute == "strength" &&
            catalog.AttributeContribution.WisdomXp == 1 &&
            catalog.Relations.Any(relation => relation.RelationKind == "substitution" &&
                                              relation.RelatedProviderExerciseId == "0289") &&
            catalog.ProgressionExerciseIds.Contains("0045") &&
            catalog.RegressionExerciseIds.Contains("0748")), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mediaStorageService.Verify(m => m.UploadAsync(
            "exercises/0025/360.gif", It.IsAny<Stream>(), "image/gif", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleTranslatesNameAndInstructionsAndMarksRawImportNormalizedWhenFullyTranslated()
    {
        // R3.1 (US-144): "barbell squat" usa apenas termos do dicionario.
        // determinístico, entao a traducao fica 100% completa e o raw import deve virar "normalized"
        // (em vez de "pending_review", que era o unico destino possivel antes desta mudanca).
        var jsonPath = Path.Combine(_sourceDirectory, "0099.json");
        await File.WriteAllTextAsync(jsonPath, """
        {
          "bodyPart": "chest",
          "equipment": "barbell",
          "id": "0099",
          "name": "barbell squat",
          "target": "quadriceps",
          "instructions": ["Barbell squat."],
          "difficulty": "intermediate",
          "category": "strength"
        }
        """);

        var result = await CreateHandler().Handle(
            new ImportExercisesCommand(BatchKey, "local_files", null, ApproveOnImport: false),
            CancellationToken.None);

        result.CatalogCreated.Should().Be(1);
        _catalogRepository.Verify(r => r.AddAsync(It.Is<ExerciseCatalog>(catalog =>
            catalog.NamePtBr == "Barra agachamento" &&
            catalog.NameOriginal == "barbell squat" &&
            catalog.InstructionsPtBr.Count == 1 &&
            catalog.InstructionsPtBr[0] != catalog.InstructionsOriginal[0]), It.IsAny<CancellationToken>()), Times.Once);
        _rawImportRepository.Verify(r => r.AddAsync(It.Is<ExerciseRawImport>(raw =>
            raw.Status == ExerciseRawImportStatus.Normalized), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleKeepsRawTextsUntranslatedWordsAndLeavesRawImportPendingReviewWhenTranslationIsIncomplete()
    {
        // "barbell bench press" tem descricao/instrucoes com palavras fora do dicionario
        // (ex.: "classic", "compound", "lie", "flat") -> traducao fica incompleta -> pending_review
        // (regressao-guarda: garante que o caso comum do dataset real nao vira normalized por engano).
        WriteSampleExercise("0025");

        var result = await CreateHandler().Handle(
            new ImportExercisesCommand(BatchKey, "local_files", null, ApproveOnImport: false),
            CancellationToken.None);

        result.CatalogCreated.Should().Be(1);
        _rawImportRepository.Verify(r => r.AddAsync(It.Is<ExerciseRawImport>(raw =>
            raw.Status == ExerciseRawImportStatus.PendingReview), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleCanApproveImportedCatalogWhenRequiredDataIsPresent()
    {
        WriteSampleExercise("0025");

        var result = await CreateHandler().Handle(
            new ImportExercisesCommand(BatchKey, "local_files", null, ApproveOnImport: true),
            CancellationToken.None);

        result.CatalogCreated.Should().Be(1);
        _catalogRepository.Verify(r => r.AddAsync(It.Is<ExerciseCatalog>(catalog =>
            catalog.IsApprovedForWorkoutGeneration &&
            catalog.SanitizationStatus == "approved"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleStoresFailedRawImportWhenJsonIsInvalid()
    {
        var jsonPath = Path.Combine(_sourceDirectory, "broken.json");
        await File.WriteAllTextAsync(jsonPath, "{ invalid json");

        var result = await CreateHandler().Handle(
            new ImportExercisesCommand(BatchKey, "local_files", null, ApproveOnImport: false),
            CancellationToken.None);

        result.RawImported.Should().Be(0);
        result.CatalogCreated.Should().Be(0);
        result.Failed.Should().Be(1);
        _rawImportRepository.Verify(r => r.AddAsync(It.Is<ExerciseRawImport>(raw =>
            raw.ProviderExerciseId == "broken" &&
            raw.Status == ExerciseRawImportStatus.Failed &&
            raw.SourceFilePath == $"{BatchKey}/broken.json"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleMarksPersistedRawImportAsFailedWhenNormalizationFails()
    {
        await File.WriteAllTextAsync(Path.Combine(_sourceDirectory, "missing-name.json"), """
        { "bodyPart":"chest", "equipment":"barbell", "id":"missing-name", "target":"chest", "instructions":["Barbell squat."] }
        """);
        ExerciseRawImport? persisted = null;
        _rawImportRepository
            .Setup(r => r.AddAsync(It.IsAny<ExerciseRawImport>(), It.IsAny<CancellationToken>()))
            .Callback<ExerciseRawImport, CancellationToken>((raw, _) => persisted = raw)
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(
            new ImportExercisesCommand(BatchKey, "local_files", null, false),
            CancellationToken.None);

        result.Failed.Should().Be(1);
        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be(ExerciseRawImportStatus.Failed);
        persisted.ErrorMessage.Should().Contain("name");
    }

    [Fact]
    public async Task HandleThrowsValidationExceptionWhenResolverReturnsNull()
    {
        _safeDirectoryResolver.Setup(r => r.Resolve("invalid-key")).Returns((string?)null);

        var act = async () => await CreateHandler().Handle(
            new ImportExercisesCommand("invalid-key", "local_files", null, false),
            CancellationToken.None);

        await act.Should().ThrowAsync<Awaken.Application.Common.Exceptions.ValidationException>()
            .WithMessage("*validation*");
    }

    [Fact]
    public async Task HandleDoesNotFailItemWhenMediaUploadThrowsAndLeavesGifUrlNull()
    {
        WriteSampleExercise("0025");
        _mediaStorageService
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("R2 unavailable"));

        var result = await CreateHandler().Handle(
            new ImportExercisesCommand(BatchKey, "local_files", null, ApproveOnImport: true),
            CancellationToken.None);

        // RN de US-236 (9.2): falha de upload de midia nao derruba o item do lote, so bloqueia a aprovacao.
        result.Failed.Should().Be(0);
        result.CatalogCreated.Should().Be(1);
        result.MediaUploaded.Should().Be(0);

        _catalogRepository.Verify(r => r.AddAsync(It.Is<ExerciseCatalog>(catalog =>
            catalog.GifUrl == null &&
            catalog.IsApprovedForWorkoutGeneration == false), It.IsAny<CancellationToken>()), Times.Once);
        _rawImportRepository.Verify(r => r.AddAsync(It.Is<ExerciseRawImport>(raw =>
            raw.MediaBaseUrl == null &&
            raw.Status == ExerciseRawImportStatus.PendingReview), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleEnrichesExistingCatalogAndPropagatesDatasetVersionToRelations()
    {
        WriteSampleExercise("0025");
        ExerciseCatalog? savedCatalog = null;
        _catalogRepository
            .Setup(r => r.AddAsync(It.IsAny<ExerciseCatalog>(), It.IsAny<CancellationToken>()))
            .Callback<ExerciseCatalog, CancellationToken>((catalog, _) => savedCatalog = catalog)
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(
            new ImportExercisesCommand(BatchKey, "local_files", null, ApproveOnImport: false),
            CancellationToken.None);

        savedCatalog.Should().NotBeNull();
        _catalogRepository.Setup(r => r.GetByProviderExerciseIdAsync(
                "local_files", "0025", It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedCatalog);
        _catalogRepository.Setup(r => r.GetByProviderExerciseIdAsync(
                "local_files", "0289", It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedCatalog);

        var eulaPath = Path.Combine(_sourceDirectory, "ExerciseDB_EULA_signed.pdf");
        await File.WriteAllBytesAsync(eulaPath, [37, 80, 68, 70]);
        _configuration.Setup(c => c["ExerciseImport:SignedEulaPath"]).Returns(eulaPath);

        var result = await CreateHandler().Handle(
            new ImportExercisesCommand(BatchKey, "local_files", null, ApproveOnImport: false, DatasetVersion: "051426"),
            CancellationToken.None);

        result.CatalogCreated.Should().Be(0);
        result.CatalogUpdated.Should().Be(1);
        result.TaxonomyApplied.Should().Be(1);
        result.RelationshipsCreated.Should().Be(4);
        _rawImportRepository.Verify(r => r.AddAsync(It.Is<ExerciseRawImport>(raw =>
            raw.DatasetVersion == "051426"), It.IsAny<CancellationToken>()), Times.Once);
        _catalogRepository.Verify(r => r.Update(It.Is<ExerciseCatalog>(catalog =>
            catalog.Relations.All(relation => relation.DatasetVersion == "051426") &&
            catalog.Relations.Count == 4 &&
            catalog.Relations.Single(relation => relation.RelatedProviderExerciseId == "0289")
                .TargetExerciseCatalogId == savedCatalog!.Id)), Times.Once);
    }

    [Fact]
    public async Task HandleLeavesEnrichedItemPendingWhenCatalogDoesNotExist()
    {
        WriteSampleExercise("0025");

        var result = await CreateHandler().Handle(
            new ImportExercisesCommand(BatchKey, "local_files", null, ApproveOnImport: false, DatasetVersion: "051426"),
            CancellationToken.None);

        result.RawImported.Should().Be(1);
        result.CatalogCreated.Should().Be(0);
        result.CatalogUpdated.Should().Be(0);
        result.Pending.Should().Be(1);
        result.MediaUploaded.Should().Be(0);
        _catalogRepository.Verify(
            r => r.AddAsync(It.IsAny<ExerciseCatalog>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mediaStorageService.Verify(
            m => m.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleKeepsExistingTaxonomyAndMarksPendingWhenEnrichmentConflicts()
    {
        WriteSampleExercise("0025");
        ExerciseCatalog? savedCatalog = null;
        _catalogRepository
            .Setup(r => r.AddAsync(It.IsAny<ExerciseCatalog>(), It.IsAny<CancellationToken>()))
            .Callback<ExerciseCatalog, CancellationToken>((catalog, _) => savedCatalog = catalog)
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(
            new ImportExercisesCommand(BatchKey, "local_files", null, ApproveOnImport: false),
            CancellationToken.None);

        savedCatalog.Should().NotBeNull();
        _catalogRepository.Setup(r => r.GetByProviderExerciseIdAsync(
                "local_files", "0025", It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedCatalog);
        File.WriteAllText(
            Path.Combine(_sourceDirectory, "0025.json"),
            SampleExerciseJson("0025").Replace(
                "\"movementPattern\": \"horizontal push\"",
                "\"movementPattern\": \"vertical pull\"",
                StringComparison.Ordinal));

        var eulaPath = Path.Combine(_sourceDirectory, "ExerciseDB_EULA_signed.pdf");
        await File.WriteAllBytesAsync(eulaPath, [37, 80, 68, 70]);
        _configuration.Setup(c => c["ExerciseImport:SignedEulaPath"]).Returns(eulaPath);

        var result = await CreateHandler().Handle(
            new ImportExercisesCommand(BatchKey, "local_files", null, ApproveOnImport: false, DatasetVersion: "051426"),
            CancellationToken.None);

        result.TaxonomyApplied.Should().Be(0);
        result.Pending.Should().Be(1);
        savedCatalog!.MovementPattern.Should().Be("horizontal_push");
        savedCatalog.SanitizationStatus.Should().Be("pending_review");
        savedCatalog.IsApprovedForWorkoutGeneration.Should().BeFalse();
    }

    [Fact]
    public async Task HandleDoesNotPublishEnrichedMediaWithoutConfiguredSignedEula()
    {
        WriteSampleExercise("0025");
        ExerciseCatalog? savedCatalog = null;
        _catalogRepository
            .Setup(r => r.AddAsync(It.IsAny<ExerciseCatalog>(), It.IsAny<CancellationToken>()))
            .Callback<ExerciseCatalog, CancellationToken>((catalog, _) => savedCatalog = catalog)
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(
            new ImportExercisesCommand(BatchKey, "local_files", null, ApproveOnImport: false),
            CancellationToken.None);
        _mediaStorageService.Invocations.Clear();
        _catalogRepository.Setup(r => r.GetByProviderExerciseIdAsync(
                "local_files", "0025", It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedCatalog);

        var result = await CreateHandler().Handle(
            new ImportExercisesCommand(BatchKey, "local_files", null, ApproveOnImport: false, DatasetVersion: "051426"),
            CancellationToken.None);

        result.MediaUploaded.Should().Be(0);
        _mediaStorageService.Verify(
            m => m.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void WriteSampleExercise(string id)
    {
        File.WriteAllText(Path.Combine(_sourceDirectory, $"{id}.json"), SampleExerciseJson(id));
        File.WriteAllBytes(Path.Combine(_sourceDirectory, $"{id}-360.gif"), [71, 73, 70, 56]);
    }

    private static string SampleExerciseJson(string id) => $$"""
    {
      "bodyPart": "chest",
      "equipment": "barbell",
      "id": "{{id}}",
      "name": "barbell bench press",
      "target": "pectorals",
      "secondaryMuscles": ["triceps", "shoulders"],
      "instructions": ["Lie flat on a bench.", "Press the barbell up."],
      "description": "Classic compound chest exercise.",
      "difficulty": "intermediate",
      "category": "strength",
      "taxonomy": {
        "movementFamily": "bench press",
        "movementPattern": "horizontal push",
        "mechanic": "compound",
        "forceType": "push",
        "planeOfMotion": "sagittal",
        "laterality": "bilateral",
        "bodyPosition": "lying",
        "benchAngle": "flat",
        "equipmentCategory": "free_weight",
        "loadType": "free_weight",
        "primaryRegion": "upper_body",
        "isCompound": true,
        "isUnilateral": false,
        "isAssisted": false,
        "isWeighted": true,
        "signals": ["external_load", "free_weight"],
        "confidence": "high"
      },
      "similarExercises": [
        { "id": "0033", "name": "barbell decline bench press", "score": 100.0, "confidence": "high", "reasons": ["same target muscle"] }
      ],
      "substitutions": [
        { "id": "0289", "name": "dumbbell bench press", "types": ["equipment_alternative"], "score": 100.0, "confidence": "high", "reasons": ["different equipment option"] }
      ],
      "progressions": [
        { "id": "0045", "name": "barbell guillotine bench press", "types": ["higher_difficulty"], "score": 100.0, "confidence": "high", "reasons": ["advanced variant"] }
      ],
      "regressions": [
        { "id": "0748", "name": "smith bench press", "types": ["lower_difficulty"], "score": 100.0, "confidence": "high", "reasons": ["beginner variant"] }
      ]
    }
    """;
}
