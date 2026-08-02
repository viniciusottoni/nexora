namespace Awaken.Contracts.Progression;

public record ProgressionResponse(
    Guid UserId,
    string Rank,
    int RankScore,
    int Level,
    long TotalXp,
    long XpToNextLevel,
    int CurrentStreakDays,
    int LongestStreakDays,
    AttributesDto Attributes,
    AttributeXpDto? AttributeXp = null);

public record AttributesDto(
    int Strength,
    int Endurance,
    int Agility,
    int Vitality,
    int Focus,
    int Wisdom);
