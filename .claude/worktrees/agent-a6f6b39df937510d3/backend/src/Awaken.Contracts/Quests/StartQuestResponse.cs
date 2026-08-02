namespace Awaken.Contracts.Quests;

public record StartQuestResponse(
    Guid QuestId,
    string QuestType,
    string Status,
    DateTime StartedAtUtc);
