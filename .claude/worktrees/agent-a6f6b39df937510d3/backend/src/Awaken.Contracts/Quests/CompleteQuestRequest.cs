namespace Awaken.Contracts.Quests;

/// US-241 §6.2: "como você se sentiu?" — too_easy | just_right | too_hard | null.
public record CompleteQuestRequest(string? PerceivedFeeling);
