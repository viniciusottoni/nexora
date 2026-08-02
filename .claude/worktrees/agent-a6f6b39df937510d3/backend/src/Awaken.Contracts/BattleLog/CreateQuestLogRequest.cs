namespace Awaken.Contracts.BattleLog;

public record CreateQuestLogRequest(
    string QuestType,
    long XpEarned,
    IReadOnlyList<string>? ItemsEarned,
    long? XpPenaltyApplied);
