using Awaken.Contracts.Progression;

namespace Awaken.Contracts.Hunter;

public record HunterProfileResponse(
    string AccessStatus,
    bool HasProgress,
    string? CardVariant = null,
    bool HasAnnualGoldenFrame = false,
    string? DisplayName = null,
    string? AvatarUrl = null,
    string? SelectedAvatarKey = null,
    string? HunterClass = null,
    string? Rank = null,
    int? RankScore = null,
    int? Level = null,
    long? Xp = null,
    long? XpToNextLevel = null,
    int? StreakDays = null,
    AttributesDto? Attributes = null,
    long? RecentDailyPenaltyXp = null,
    DateTime? RecentDailyPenaltyQuestDateUtc = null,
    AttributeXpDto? AttributeXp = null,
    IReadOnlyList<StreakCalendarDayResponse>? StreakCalendarDays = null,
    string? EquippedFrameKey = null,
    string? EquippedAuraKey = null,
    string? EquippedBackgroundKey = null);

public record StreakCalendarDayResponse(
    DateTime DateUtc,
    string Status);
