namespace Awaken.Domain.Entities.Exercises;

public enum ExerciseRawImportStatus
{
    Imported,
    Failed,
    Normalized,
    PendingReview,
    Approved,
    Rejected,
    Deprecated
}
