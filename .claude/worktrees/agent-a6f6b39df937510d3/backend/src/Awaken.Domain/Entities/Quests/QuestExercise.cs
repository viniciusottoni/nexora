using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Quests;

/// US-057/US-058: unidade de progresso materializada no Start da quest a partir do WorkoutJson.
public class QuestExercise : BaseEntity
{
    public Guid QuestId { get; private set; }
    public int Order { get; private set; }
    public string Status { get; private set; } = "pending"; // pending | completed
    public string Name { get; private set; } = string.Empty;
    public string? ExerciseCatalogProviderId { get; private set; } // null em fallback
    public int Sets { get; private set; }
    public int RepsMin { get; private set; }
    public int? RepsMax { get; private set; }
    public int? RestSeconds { get; private set; }
    public string? TargetRpe { get; private set; }
    public string? VideoUrl { get; private set; }

    // Snapshot congelado no momento do Start, usado pra conceder recompensa.
    public long XpReward { get; private set; }
    public int StrengthXp { get; private set; }
    public int AgilityXp { get; private set; }
    public int EnduranceXp { get; private set; }
    public int VitalityXp { get; private set; }
    public int FocusXp { get; private set; }
    public int WisdomXp { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }
    public long XpEarned { get; private set; } // 0 enquanto pending

    // US-130: level-ups de atributo ocorridos na conclusão deste exercício.
    public int StrengthLevelUps { get; private set; }
    public int AgilityLevelUps { get; private set; }
    public int EnduranceLevelUps { get; private set; }
    public int VitalityLevelUps { get; private set; }
    public int FocusLevelUps { get; private set; }
    public int WisdomLevelUps { get; private set; }

    private QuestExercise() { }

    public static QuestExercise Create(Guid questId, int order, QuestExerciseSeed seed) => new()
    {
        QuestId = questId,
        Order = order,
        Name = seed.Name,
        ExerciseCatalogProviderId = seed.ExerciseCatalogProviderId,
        Sets = seed.Sets,
        RepsMin = seed.RepsMin,
        RepsMax = seed.RepsMax,
        RestSeconds = seed.RestSeconds,
        TargetRpe = seed.TargetRpe,
        VideoUrl = seed.VideoUrl,
        XpReward = seed.XpReward,
        StrengthXp = seed.StrengthXp,
        AgilityXp = seed.AgilityXp,
        EnduranceXp = seed.EnduranceXp,
        VitalityXp = seed.VitalityXp,
        FocusXp = seed.FocusXp,
        WisdomXp = seed.WisdomXp,
    };

    /// US-064/US-065: idempotente — retorna true só na 1a conclusão.
    public bool MarkCompleted(DateTime utcNow, long calculatedXp)
    {
        if (Status == "completed") return false;

        Status = "completed";
        CompletedAtUtc = utcNow;
        XpEarned = calculatedXp;
        return true;
    }

    /// US-230: substitui este exercício por outro compatível (Pergaminho da
    /// Substituição). Só permitido enquanto pending — uma vez concluído, o
    /// XpEarned/CompletedAtUtc já são histórico e não podem ser trocados.
    public void ReplaceWith(QuestExerciseSeed newSeed, DateTime utcNow)
    {
        if (Status != "pending")
            throw new InvalidOperationException("Só é possível substituir um exercício pendente.");

        Name = newSeed.Name;
        ExerciseCatalogProviderId = newSeed.ExerciseCatalogProviderId;
        Sets = newSeed.Sets;
        RepsMin = newSeed.RepsMin;
        RepsMax = newSeed.RepsMax;
        RestSeconds = newSeed.RestSeconds;
        TargetRpe = newSeed.TargetRpe;
        VideoUrl = newSeed.VideoUrl;
        XpReward = newSeed.XpReward;
        StrengthXp = newSeed.StrengthXp;
        AgilityXp = newSeed.AgilityXp;
        EnduranceXp = newSeed.EnduranceXp;
        VitalityXp = newSeed.VitalityXp;
        FocusXp = newSeed.FocusXp;
        WisdomXp = newSeed.WisdomXp;
        UpdatedAtUtc = utcNow;
    }

    /// US-130: registra quantos level-ups de atributo ocorreram neste exercício.
    public void SetAttributeLevelUps(
        int strength, int agility, int endurance, int vitality, int focus, int wisdom)
    {
        StrengthLevelUps = strength;
        AgilityLevelUps = agility;
        EnduranceLevelUps = endurance;
        VitalityLevelUps = vitality;
        FocusLevelUps = focus;
        WisdomLevelUps = wisdom;
    }
}
