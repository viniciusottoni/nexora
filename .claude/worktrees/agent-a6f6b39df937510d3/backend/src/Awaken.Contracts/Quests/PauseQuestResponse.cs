namespace Awaken.Contracts.Quests;

public record PauseQuestResponse(
    Guid QuestId,
    string QuestType,
    string Status,
    DateTime PausedAtUtc);
