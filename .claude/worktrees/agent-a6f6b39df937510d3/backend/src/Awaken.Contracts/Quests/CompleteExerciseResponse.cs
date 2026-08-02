namespace Awaken.Contracts.Quests;

public record CompleteExerciseResponse(
    Guid QuestExerciseId,
    string Status,
    long XpEarned,
    long TotalXp,
    int EffectiveDifficulty,
    QuestAttributeXpDto AttributeXpEarned,
    QuestVisibleAttributeImpactsDto AttributePointsGranted,
    bool AlreadyCompleted,
    int LevelsGained = 0,
    bool RankChanged = false,
    string? NewRank = null,
    IReadOnlyList<string>? AttributeLevelUps = null);
