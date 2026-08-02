namespace Awaken.Contracts.Exercises;

public record ImportExercisesResponse(
    string ImportBatchId,
    int RawImported,
    int CatalogCreated,
    int CatalogUpdated,
    int Failed,
    int MediaUploaded = 0,
    int TaxonomyApplied = 0,
    int RelationshipsCreated = 0,
    int Pending = 0);
