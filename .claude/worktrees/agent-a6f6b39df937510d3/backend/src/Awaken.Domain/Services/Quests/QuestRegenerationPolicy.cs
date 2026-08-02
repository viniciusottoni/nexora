namespace Awaken.Domain.Services.Quests;

/// US-048 RN-001: limite diario justo de regeneracoes da quest, antes de
/// exigir o consumo do item "Pergaminho da Reforja" para tentar novamente.
public static class QuestRegenerationPolicy
{
    public const int DailyFreeLimit = 1;
}
