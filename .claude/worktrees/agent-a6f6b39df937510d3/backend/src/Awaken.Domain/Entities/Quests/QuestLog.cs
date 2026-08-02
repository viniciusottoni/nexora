using System.Text.Json;
using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Quests;

/// US-062: registro confiavel da conclusao de uma quest. Fonte de verdade para
/// historico (EPIC-011), recompensa (US-063) e auditoria. Uma quest gera no
/// maximo um QuestLog (RN-007).
public class QuestLog : BaseEntity
{
    public Guid QuestId { get; private set; }
    public Guid UserId { get; private set; }
    public string QuestType { get; private set; } = "daily";
    public long XpEarned { get; private set; }

    public int StrengthXpEarned { get; private set; }
    public int AgilityXpEarned { get; private set; }
    public int EnduranceXpEarned { get; private set; }
    public int VitalityXpEarned { get; private set; }
    public int FocusXpEarned { get; private set; }
    public int WisdomXpEarned { get; private set; }

    public int StrengthPointsGranted { get; private set; }
    public int AgilityPointsGranted { get; private set; }
    public int EndurancePointsGranted { get; private set; }
    public int VitalityPointsGranted { get; private set; }
    public int FocusPointsGranted { get; private set; }

    public string? ItemsEarnedJson { get; private set; }
    public long? XpPenaltyApplied { get; private set; }
    public DateTime CompletedAtUtc { get; private set; }

    // US-241 §6.2: sentimento percebido pelo usuário ao concluir a quest — único
    // sinal real de desempenho usado pela progressão semanal (ver PerceivedFeelings).
    public string? PerceivedFeeling { get; private set; }

    public IReadOnlyList<string> ItemsEarned =>
        ItemsEarnedJson is null ? [] : JsonSerializer.Deserialize<List<string>>(ItemsEarnedJson)!;

    private QuestLog() { }

    public static QuestLog Create(
        Guid questId,
        Guid userId,
        string questType,
        long xpEarned,
        int strengthXpEarned,
        int agilityXpEarned,
        int enduranceXpEarned,
        int vitalityXpEarned,
        int focusXpEarned,
        int wisdomXpEarned,
        int strengthPointsGranted,
        int agilityPointsGranted,
        int endurancePointsGranted,
        int vitalityPointsGranted,
        int focusPointsGranted,
        IReadOnlyList<string> itemsEarned,
        DateTime completedAtUtc,
        long? xpPenaltyApplied = null,
        string? perceivedFeeling = null)
    {
        return new QuestLog
        {
            QuestId = questId,
            UserId = userId,
            QuestType = questType,
            XpEarned = xpEarned,
            StrengthXpEarned = strengthXpEarned,
            AgilityXpEarned = agilityXpEarned,
            EnduranceXpEarned = enduranceXpEarned,
            VitalityXpEarned = vitalityXpEarned,
            FocusXpEarned = focusXpEarned,
            WisdomXpEarned = wisdomXpEarned,
            StrengthPointsGranted = strengthPointsGranted,
            AgilityPointsGranted = agilityPointsGranted,
            EndurancePointsGranted = endurancePointsGranted,
            VitalityPointsGranted = vitalityPointsGranted,
            FocusPointsGranted = focusPointsGranted,
            ItemsEarnedJson = itemsEarned.Count > 0 ? JsonSerializer.Serialize(itemsEarned) : null,
            XpPenaltyApplied = xpPenaltyApplied,
            CompletedAtUtc = completedAtUtc,
            PerceivedFeeling = perceivedFeeling,
        };
    }
}
