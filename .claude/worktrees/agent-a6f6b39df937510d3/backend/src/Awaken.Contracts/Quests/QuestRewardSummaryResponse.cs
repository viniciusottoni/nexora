namespace Awaken.Contracts.Quests;

public record QuestRewardSummaryResponse(
    Guid QuestId,
    string QuestType,
    long XpEarned,
    QuestAttributeXpDto AttributeXpEarned,
    QuestVisibleAttributeImpactsDto AttributePointsGranted,
    int StreakDays,
    IReadOnlyList<string> ItemsEarned,
    IReadOnlyList<string>? AttributeLevelUps = null,
    IReadOnlyList<QuestAttributeLevelUpDto>? AttributeLevelUpDetails = null,
    IReadOnlyList<QuestItemRewardDto>? ItemRewards = null);

public record QuestAttributeLevelUpDto(
    string Attribute,
    int NewLevel,
    string Source);

public record QuestItemRewardDto(
    string ItemId,
    string Rarity,
    string Source);
