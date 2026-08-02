namespace Awaken.Domain.Entities.Quests;

/// US-056/US-057: dados congelados no momento do Start, usados para materializar QuestExercise.
public record QuestExerciseSeed(
    string Name,
    string? ExerciseCatalogProviderId,
    int Sets,
    int RepsMin,
    int? RepsMax,
    int? RestSeconds,
    string? TargetRpe,
    string? VideoUrl,
    long XpReward,
    int StrengthXp,
    int AgilityXp,
    int EnduranceXp,
    int VitalityXp,
    int FocusXp,
    int WisdomXp);
