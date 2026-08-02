namespace Awaken.Contracts.Exercises;

public record ImportExercisesRequest
{
    public ImportExercisesRequest()
    {
    }

    public ImportExercisesRequest(
        string batchKey,
        string? provider = null,
        int? maxFiles = null,
        bool approveOnImport = false,
        string? datasetVersion = null)
    {
        BatchKey = batchKey;
        Provider = provider;
        MaxFiles = maxFiles;
        ApproveOnImport = approveOnImport;
        DatasetVersion = datasetVersion;
    }

    public string BatchKey { get; init; } = string.Empty;
    public string? Provider { get; init; }
    public int? MaxFiles { get; init; }
    public bool ApproveOnImport { get; init; }

    /// <summary>US-236 — versão do dataset enriquecido de origem (RN-001).</summary>
    public string? DatasetVersion { get; init; }
}
