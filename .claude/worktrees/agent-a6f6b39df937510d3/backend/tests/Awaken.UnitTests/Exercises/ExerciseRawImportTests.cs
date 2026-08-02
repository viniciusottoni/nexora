using Awaken.Domain.Entities.Exercises;
using FluentAssertions;

namespace Awaken.UnitTests.Exercises;

public class ExerciseRawImportTests
{
    private static readonly DateTime UtcNow = new(2026, 6, 22, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateImportedSetsStatusImportedAndTraceabilityFields()
    {
        var rawImport = ExerciseRawImport.CreateImported(
            providerName: "ExerciseDB",
            providerExerciseId: "ex-001",
            providerVersion: "v1",
            rawJson: "{\"id\":\"ex-001\"}",
            importBatchId: "imp_20260622",
            sourceUrl: "https://provider.example/exercises/ex-001",
            sourceFilePath: "C:\\sample-set\\ex-001.json",
            mediaBaseUrl: "https://provider.example/media/ex-001.gif",
            mediaLicenseInfo: "CC-BY-4.0",
            utcNow: UtcNow);

        rawImport.Status.Should().Be(ExerciseRawImportStatus.Imported);
        rawImport.ProviderName.Should().Be("ExerciseDB");
        rawImport.ProviderExerciseId.Should().Be("ex-001");
        rawImport.RawJson.Should().Be("{\"id\":\"ex-001\"}");
        rawImport.ImportBatchId.Should().Be("imp_20260622");
        rawImport.SourceUrl.Should().Be("https://provider.example/exercises/ex-001");
        rawImport.SourceFilePath.Should().Be("C:\\sample-set\\ex-001.json");
        rawImport.MediaBaseUrl.Should().Be("https://provider.example/media/ex-001.gif");
        rawImport.MediaLicenseInfo.Should().Be("CC-BY-4.0");
        rawImport.ImportedAtUtc.Should().Be(UtcNow);
        rawImport.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void CreateFailedSetsStatusFailedAndErrorMessage()
    {
        var rawImport = ExerciseRawImport.CreateFailed(
            providerName: "ExerciseDB",
            providerExerciseId: "ex-002",
            importBatchId: "imp_20260622",
            errorMessage: "Timeout calling provider",
            utcNow: UtcNow,
            rawJson: "{\"id\":\"ex-002\",\"name\":\"Broken\"}",
            sourceUrl: "https://provider.example/exercises/ex-002",
            sourceFilePath: "C:\\sample-set\\ex-002.json",
            mediaBaseUrl: "https://provider.example/media/ex-002.gif",
            mediaLicenseInfo: "CC-BY-4.0");

        rawImport.Status.Should().Be(ExerciseRawImportStatus.Failed);
        rawImport.ErrorMessage.Should().Be("Timeout calling provider");
        rawImport.RawJson.Should().Be("{\"id\":\"ex-002\",\"name\":\"Broken\"}");
        rawImport.SourceUrl.Should().Be("https://provider.example/exercises/ex-002");
        rawImport.SourceFilePath.Should().Be("C:\\sample-set\\ex-002.json");
        rawImport.MediaBaseUrl.Should().Be("https://provider.example/media/ex-002.gif");
        rawImport.MediaLicenseInfo.Should().Be("CC-BY-4.0");
        rawImport.ImportedAtUtc.Should().Be(UtcNow);
    }

    [Fact]
    public void MarkPendingReviewMovesRawImportToReviewPipeline()
    {
        var rawImport = ExerciseRawImport.CreateImported(
            providerName: "local_files",
            providerExerciseId: "0025",
            providerVersion: null,
            rawJson: "{\"id\":\"0025\"}",
            importBatchId: "imp_20260622",
            sourceUrl: null,
            sourceFilePath: "C:\\sample-set\\0025.json",
            mediaBaseUrl: "C:\\sample-set\\0025-360.gif",
            mediaLicenseInfo: null,
            utcNow: UtcNow);

        rawImport.MarkPendingReview(UtcNow);

        rawImport.Status.Should().Be(ExerciseRawImportStatus.PendingReview);
    }

    /// <summary>R3.3 (US-149) — usa os valores de <see cref="ExerciseRawImportStatus"/> que já existiam
    /// no enum (Rejected/Deprecated) mas nunca eram alcançados por nenhum método até esta mudança.</summary>
    [Fact]
    public void MarkRejectedMovesRawImportToRejectedAndRecordsReason()
    {
        var rawImport = ExerciseRawImport.CreateImported(
            providerName: "local_files",
            providerExerciseId: "0025",
            providerVersion: null,
            rawJson: "{\"id\":\"0025\"}",
            importBatchId: "imp_20260622",
            sourceUrl: null,
            sourceFilePath: "C:\\sample-set\\0025.json",
            mediaBaseUrl: null,
            mediaLicenseInfo: null,
            utcNow: UtcNow);

        rawImport.MarkRejected("sem sentido", UtcNow.AddHours(1));

        rawImport.Status.Should().Be(ExerciseRawImportStatus.Rejected);
        rawImport.ErrorMessage.Should().Be("sem sentido");
    }

    [Fact]
    public void MarkDeprecatedMovesRawImportToDeprecated()
    {
        var rawImport = ExerciseRawImport.CreateImported(
            providerName: "local_files",
            providerExerciseId: "0025",
            providerVersion: null,
            rawJson: "{\"id\":\"0025\"}",
            importBatchId: "imp_20260622",
            sourceUrl: null,
            sourceFilePath: "C:\\sample-set\\0025.json",
            mediaBaseUrl: null,
            mediaLicenseInfo: null,
            utcNow: UtcNow);
        rawImport.MarkApproved(UtcNow);

        rawImport.MarkDeprecated(UtcNow.AddDays(1));

        rawImport.Status.Should().Be(ExerciseRawImportStatus.Deprecated);
    }
}
