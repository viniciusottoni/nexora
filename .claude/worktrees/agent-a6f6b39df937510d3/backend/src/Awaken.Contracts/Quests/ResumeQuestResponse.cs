namespace Awaken.Contracts.Quests;

public record ResumeQuestResponse(
    Guid QuestId,
    string QuestType,
    string Status,
    DateTime ResumedAtUtc);
