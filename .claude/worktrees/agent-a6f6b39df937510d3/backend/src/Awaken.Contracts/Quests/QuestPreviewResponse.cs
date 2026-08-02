namespace Awaken.Contracts.Quests;

public record QuestPreviewResponse(
    Guid QuestId,
    string QuestType,
    string TrainingType,
    long EstimatedXp,
    int EstimatedDurationMinutes,
    bool CanChangeTrainingType,
    WorkoutDto? Workout);


