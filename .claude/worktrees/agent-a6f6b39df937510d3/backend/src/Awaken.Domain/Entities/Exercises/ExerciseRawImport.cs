using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Exercises;

public class ExerciseRawImport : BaseEntity
{
    public string ProviderName { get; private set; } = string.Empty;
    public string ProviderExerciseId { get; private set; } = string.Empty;
    public string? ProviderVersion { get; private set; }
    public string RawJson { get; private set; } = string.Empty;
    public DateTime ImportedAtUtc { get; private set; }
    public string ImportBatchId { get; private set; } = string.Empty;
    public string? SourceUrl { get; private set; }
    public string? SourceFilePath { get; private set; }
    public string? MediaBaseUrl { get; private set; }
    public string? MediaLicenseInfo { get; private set; }
    public ExerciseRawImportStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    /// <summary>US-236 — versão do dataset enriquecido de origem (RN-001), rastreabilidade do lote.</summary>
    public string? DatasetVersion { get; private set; }

    private ExerciseRawImport() { }

    public static ExerciseRawImport CreateImported(
        string providerName,
        string providerExerciseId,
        string? providerVersion,
        string rawJson,
        string importBatchId,
        string? sourceUrl,
        string? sourceFilePath,
        string? mediaBaseUrl,
        string? mediaLicenseInfo,
        DateTime utcNow,
        string? datasetVersion = null)
    {
        return new ExerciseRawImport
        {
            ProviderName = providerName,
            ProviderExerciseId = providerExerciseId,
            ProviderVersion = providerVersion,
            RawJson = rawJson,
            ImportBatchId = importBatchId,
            SourceUrl = sourceUrl,
            SourceFilePath = sourceFilePath,
            MediaBaseUrl = mediaBaseUrl,
            MediaLicenseInfo = mediaLicenseInfo,
            ImportedAtUtc = utcNow,
            Status = ExerciseRawImportStatus.Imported,
            DatasetVersion = datasetVersion,
        };
    }

    public static ExerciseRawImport CreateFailed(
        string providerName,
        string providerExerciseId,
        string importBatchId,
        string errorMessage,
        DateTime utcNow,
        string? rawJson = null,
        string? sourceUrl = null,
        string? sourceFilePath = null,
        string? mediaBaseUrl = null,
        string? mediaLicenseInfo = null,
        string? datasetVersion = null)
    {
        return new ExerciseRawImport
        {
            ProviderName = providerName,
            ProviderExerciseId = providerExerciseId,
            ImportBatchId = importBatchId,
            RawJson = rawJson ?? string.Empty,
            SourceUrl = sourceUrl,
            SourceFilePath = sourceFilePath,
            MediaBaseUrl = mediaBaseUrl,
            MediaLicenseInfo = mediaLicenseInfo,
            ImportedAtUtc = utcNow,
            Status = ExerciseRawImportStatus.Failed,
            ErrorMessage = errorMessage,
            DatasetVersion = datasetVersion,
        };
    }

    public void MarkFailed(string errorMessage, DateTime utcNow)
    {
        Status = ExerciseRawImportStatus.Failed;
        ErrorMessage = errorMessage;
        UpdatedAtUtc = utcNow;
    }

    public void MarkNormalized(DateTime utcNow)
    {
        Status = ExerciseRawImportStatus.Normalized;
        UpdatedAtUtc = utcNow;
    }

    public void SetMediaBaseUrl(string mediaBaseUrl, DateTime utcNow)
    {
        MediaBaseUrl = mediaBaseUrl;
        UpdatedAtUtc = utcNow;
    }

    public void RefreshImportedSnapshot(
        string rawJson,
        string importBatchId,
        string? sourceFilePath,
        string? mediaLicenseInfo,
        string? datasetVersion,
        DateTime utcNow)
    {
        RawJson = rawJson;
        ImportBatchId = importBatchId;
        SourceFilePath = sourceFilePath;
        MediaLicenseInfo = mediaLicenseInfo;
        DatasetVersion = datasetVersion;
        ImportedAtUtc = utcNow;
        ErrorMessage = null;
        Status = ExerciseRawImportStatus.Imported;
        UpdatedAtUtc = utcNow;
    }

    public void MarkPendingReview(DateTime utcNow)
    {
        Status = ExerciseRawImportStatus.PendingReview;
        UpdatedAtUtc = utcNow;
    }

    public void MarkApproved(DateTime utcNow)
    {
        Status = ExerciseRawImportStatus.Approved;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>US-149 (R3.3) — reflete no raw import a rejeição de curadoria do catálogo
    /// (<see cref="Exercises.ExerciseCatalog.Reject"/>). Reaproveita <see cref="ErrorMessage"/> como
    /// campo de motivo, no mesmo padrão já usado por <see cref="CreateFailed"/>.</summary>
    public void MarkRejected(string reason, DateTime utcNow)
    {
        Status = ExerciseRawImportStatus.Rejected;
        ErrorMessage = reason;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>US-149 (R3.3) — retira o exercício de novas seleções sem afetar histórico
    /// (quests já geradas continuam referenciando o mesmo <c>ExerciseCatalog</c>).</summary>
    public void MarkDeprecated(DateTime utcNow)
    {
        Status = ExerciseRawImportStatus.Deprecated;
        UpdatedAtUtc = utcNow;
    }
}
