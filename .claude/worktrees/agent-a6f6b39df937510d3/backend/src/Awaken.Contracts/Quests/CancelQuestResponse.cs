namespace Awaken.Contracts.Quests;

public record CancelQuestResponse(
    Guid QuestId,
    string QuestType,
    string Status,
    DateTime CancelledAtUtc);
