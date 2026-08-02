namespace Awaken.Contracts.Quests;

public record ValidateTrainingTypeChangeResponse(
    bool Valid,
    long EstimatedXp,
    int EstimatedDurationMinutes);
