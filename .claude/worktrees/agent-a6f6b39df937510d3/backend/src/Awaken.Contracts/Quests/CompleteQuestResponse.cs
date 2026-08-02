namespace Awaken.Contracts.Quests;

public record CompleteQuestResponse(
    Guid QuestId,
    string QuestType,
    string Status,
    long XpEarned,
    QuestAttributeXpDto AttributeXpEarned,
    QuestVisibleAttributeImpactsDto AttributePointsGranted,
    IReadOnlyList<string> ItemsEarned,
    DateTime CompletedAtUtc);
