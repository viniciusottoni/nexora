using Awaken.Domain.Common;
using Awaken.Domain.Services.Progression;

namespace Awaken.Domain.Entities.Progression;

public class HunterProgression : BaseEntity
{
    public const string DefaultHunterClass = "beginner_hunter";
    private const int DailyMissPenaltyStepXp = 10;

    /// US-130: XP interno necessário para subir 1 Level de atributo.
    public const int AttributeXpPerLevel = 10;

    /// US-130: Level máximo de cada atributo.
    public const int MaxAttributeLevel = 10;

    /// US-154: limite mensal de ganho de RankScore.
    public const int MonthlyRankScoreLimit = 24;

    private static readonly Dictionary<int, int> StreakMilestoneBonuses = new()
    {
        { 7, 1 }, { 30, 3 }, { 90, 8 }, { 180, 15 }, { 365, 35 },
    };

    public Guid UserId { get; private set; }
    public string Rank { get; private set; } = "E";
    public int RankScore { get; private set; }

    /// US-069: bônus de RankScore acumulado por marcos de streak (nunca revertido).
    public int StreakRankScoreBonus { get; private set; }

    /// US-154: ganho acumulado de RankScore no mês corrente (reset mensal).
    public int MonthlyRankScoreGain { get; private set; }

    /// US-154: mês de referência para reset mensal (formato YYYYMM, ex: 202606).
    public int MonthlyRankScoreResetYearMonth { get; private set; }

    public string HunterClass { get; private set; } = DefaultHunterClass;
    public int Level { get; private set; } = 1;
    public long TotalXp { get; private set; }
    public long XpToNextLevel { get; private set; } = 100;
    public int CurrentStreakDays { get; private set; }
    public int LongestStreakDays { get; private set; }
    public DateTime? LastQuestCompletedAtUtc { get; private set; }
    public int ConsecutiveMissedDailyDays { get; private set; }
    public long? RecentDailyPenaltyXp { get; private set; }
    public DateTime? RecentDailyPenaltyQuestDateUtc { get; private set; }

    /// US-230: snapshot do streak perdido no último reset (antes de zerar),
    /// consultado pelo Amuleto de Retorno para saber quantos dias restaurar.
    public int? RecentLostStreakDays { get; private set; }

    /// US-230: cosméticos equipados (moldura/aura/fundo), null = nenhum.
    public string? EquippedFrameKey { get; private set; }
    public string? EquippedAuraKey { get; private set; }
    public string? EquippedBackgroundKey { get; private set; }

    // Attribute levels (0–10)
    public int Strength { get; private set; } = 1;
    public int Endurance { get; private set; } = 1;
    public int Agility { get; private set; } = 1;
    public int Vitality { get; private set; } = 1;
    public int Focus { get; private set; } = 1;
    public int Wisdom { get; private set; } = 1;

    // US-130: XP interno por atributo (0–9); acumula até 10 para +1 Level.
    public int StrengthXp { get; private set; }
    public int EnduranceXp { get; private set; }
    public int AgilityXp { get; private set; }
    public int VitalityXp { get; private set; }
    public int FocusXp { get; private set; }
    public int WisdomXp { get; private set; }

    private HunterProgression() { }

    public static long CalculateXpToNextLevel(int level) =>
        (long)Math.Floor(100.0 * Math.Pow(level, 1.5));

    public static HunterProgression Create(Guid userId)
    {
        const int defaultAttr = 1;
        var rankScore = defaultAttr * 6;
        return new()
        {
            UserId = userId,
            RankScore = rankScore,
            Rank = RankCalculator.CalculateRank(rankScore),
        };
    }

    public static HunterProgression CreateFromOnboarding(
        Guid userId,
        int strength,
        int agility,
        int endurance,
        int vitality,
        int focus,
        int wisdom)
    {
        var rankScore = strength + agility + endurance + vitality + focus + wisdom;
        return new()
        {
            UserId = userId,
            Level = 1,
            Strength = strength,
            Agility = agility,
            Endurance = endurance,
            Vitality = vitality,
            Focus = focus,
            Wisdom = wisdom,
            RankScore = rankScore,
            Rank = RankCalculator.CalculateRank(rankScore),
        };
    }

    /// US-066: acumula XP e aplica level-up(s) enquanto o limiar for atingido.
    public void AddXp(long amount, DateTime utcNow)
    {
        TotalXp += amount;
        while (TotalXp >= XpToNextLevel)
        {
            TotalXp -= XpToNextLevel;
            Level++;
            XpToNextLevel = CalculateXpToNextLevel(Level);
        }
        UpdatedAtUtc = utcNow;
    }

    /// US-058/US-068: concede pontos de atributo ao concluir um QuestExercise.
    /// US-067/US-154: aplica diminishing returns ao ganho de RankScore.
    public RankScoreGainAudit AddAttributePoints(
        int strength, int agility, int endurance, int vitality, int focus, int wisdom,
        DateTime utcNow)
    {
        var rawGain = strength + agility + endurance + vitality + focus + wisdom;
        Strength += strength;
        Agility += agility;
        Endurance += endurance;
        Vitality += vitality;
        Focus += focus;
        Wisdom += wisdom;

        return ApplyRankScoreGain(rawGain, externalMultiplier: 1.0m, utcNow);
    }

    /// US-130: acumula XP interno por atributo e sobe o Level quando atingir 10.
    /// US-154/US-155: aplica diminishing returns e multiplier externo (anti-abuso) ao ganho de RankScore.
    /// Retorna resultado com level-ups e auditoria de RankScore.
    public AddAttributeXpResult AddAttributeXp(
        int strength, int agility, int endurance, int vitality, int focus, int wisdom,
        decimal externalMultiplier, DateTime utcNow)
    {
        var (sLvl, sXp, sUps) = ApplyAttrXp(Strength, StrengthXp, strength);
        var (aLvl, aXp, aUps) = ApplyAttrXp(Agility, AgilityXp, agility);
        var (eLvl, eXp, eUps) = ApplyAttrXp(Endurance, EnduranceXp, endurance);
        var (vLvl, vXp, vUps) = ApplyAttrXp(Vitality, VitalityXp, vitality);
        var (fLvl, fXp, fUps) = ApplyAttrXp(Focus, FocusXp, focus);
        var (wLvl, wXp, wUps) = ApplyAttrXp(Wisdom, WisdomXp, wisdom);

        Strength = sLvl; StrengthXp = sXp;
        Agility = aLvl; AgilityXp = aXp;
        Endurance = eLvl; EnduranceXp = eXp;
        Vitality = vLvl; VitalityXp = vXp;
        Focus = fLvl; FocusXp = fXp;
        Wisdom = wLvl; WisdomXp = wXp;

        var rawGain = sUps + aUps + eUps + vUps + fUps + wUps;
        var rankScoreAudit = ApplyRankScoreGain(rawGain, externalMultiplier, utcNow);

        var levelUps = new AttributeLevelUpsResult(sUps, aUps, eUps, vUps, fUps, wUps);
        return new AddAttributeXpResult(levelUps, rankScoreAudit);
    }

    private static (int level, int xp, int levelUps) ApplyAttrXp(int level, int xp, int gained)
    {
        if (gained <= 0) return (level, xp, 0);

        xp += gained;
        var levelUps = 0;
        while (xp >= AttributeXpPerLevel && level < MaxAttributeLevel)
        {
            xp -= AttributeXpPerLevel;
            level++;
            levelUps++;
        }

        // Cap: ao atingir MaxAttributeLevel, XP interno não acumula mais.
        if (level >= MaxAttributeLevel)
            xp = 0;

        return (level, xp, levelUps);
    }

    /// US-069 RN-002: aplica bônus de RankScore ao atingir marcos de streak.
    /// US-154 RN-005: bônus de streak também sujeito a diminishing returns e limite mensal.
    /// Retorna resultado com bônus bruto e auditoria de RankScore (null se nenhum marco atingido).
    public StreakMilestoneBonusResult TryApplyStreakMilestoneBonus(DateTime utcNow)
    {
        if (!StreakMilestoneBonuses.TryGetValue(CurrentStreakDays, out var bonus))
            return new StreakMilestoneBonusResult(0, null);

        StreakRankScoreBonus += bonus;
        var audit = ApplyRankScoreGain(bonus, externalMultiplier: 1.0m, utcNow);
        return new StreakMilestoneBonusResult(bonus, audit);
    }

    /// US-070 RN-002: reinicia o streak quando a daily não foi concluída.
    /// US-230: antes de zerar, guarda snapshot do streak perdido (só se >0)
    /// para o Amuleto de Retorno poder restaurá-lo depois (mesmo padrão de
    /// RecentDailyPenaltyXp/RecentDailyPenaltyQuestDateUtc).
    public void ResetStreak(DateTime utcNow)
    {
        if (CurrentStreakDays > 0)
            RecentLostStreakDays = CurrentStreakDays;

        CurrentStreakDays = 0;
        UpdatedAtUtc = utcNow;
    }

    /// US-230: restaura o streak perdido via Amuleto de Retorno — o usuário
    /// treinou hoje após perder o streak ontem, então o dia de hoje soma ao
    /// streak anterior em vez de reiniciar do zero.
    public void RestoreStreak(int daysToRestore, DateTime completedAtUtc)
    {
        CurrentStreakDays = daysToRestore;
        if (CurrentStreakDays > LongestStreakDays)
            LongestStreakDays = CurrentStreakDays;

        LastQuestCompletedAtUtc = completedAtUtc;
        ConsecutiveMissedDailyDays = 0;
        RecentDailyPenaltyXp = null;
        RecentDailyPenaltyQuestDateUtc = null;
        RecentLostStreakDays = null;
        UpdatedAtUtc = completedAtUtc;
    }

    /// US-230: troca a classe do Hunter — desbloqueada por posse de Pack (ver
    /// HunterClassCatalog) e aplicada via Pergaminho da Classe ou ao abrir um Pack.
    public void ChangeClass(string newClass, DateTime utcNow)
    {
        HunterClass = newClass;
        UpdatedAtUtc = utcNow;
    }

    /// US-230: equipa (ou remove, se itemKey for null) um cosmético comprado
    /// na loja. Validação de posse/categoria é responsabilidade do handler.
    public void EquipFrame(string? itemKey, DateTime utcNow)
    {
        EquippedFrameKey = itemKey;
        UpdatedAtUtc = utcNow;
    }

    public void EquipAura(string? itemKey, DateTime utcNow)
    {
        EquippedAuraKey = itemKey;
        UpdatedAtUtc = utcNow;
    }

    public void EquipBackground(string? itemKey, DateTime utcNow)
    {
        EquippedBackgroundKey = itemKey;
        UpdatedAtUtc = utcNow;
    }

    /// US-132 (RN-001/RN-002/RN-003): -10 XP por dia consecutivo sem completar a daily, com piso de 0.
    public long ApplyDailyMissPenalty(DateTime utcNow, DateTime? questDateUtc = null)
    {
        ConsecutiveMissedDailyDays++;
        var penalty = (long)ConsecutiveMissedDailyDays * DailyMissPenaltyStepXp;
        var applied = Math.Min(penalty, TotalXp);

        TotalXp -= applied;
        RecentDailyPenaltyXp = applied;
        RecentDailyPenaltyQuestDateUtc = questDateUtc?.Date ?? utcNow.Date;
        UpdatedAtUtc = utcNow;
        return applied;
    }

    public void UpdateStreakAfterQuestCompletion(DateTime completedAtUtc)
    {
        var today = completedAtUtc.Date;
        var yesterday = today.AddDays(-1);

        if (LastQuestCompletedAtUtc?.Date == yesterday)
            CurrentStreakDays++;
        else if (LastQuestCompletedAtUtc?.Date != today)
            CurrentStreakDays = 1;

        if (CurrentStreakDays > LongestStreakDays)
            LongestStreakDays = CurrentStreakDays;

        LastQuestCompletedAtUtc = completedAtUtc;
        ConsecutiveMissedDailyDays = 0;
        RecentDailyPenaltyXp = null;
        RecentDailyPenaltyQuestDateUtc = null;
        RecentLostStreakDays = null;
        UpdatedAtUtc = completedAtUtc;
    }

    // US-154: multiplier por Rank para diminishing returns.
    private static decimal GetRankMultiplier(string rank) => rank switch
    {
        "B" => 0.90m,
        "A" => 0.80m,
        "S" => 0.70m,
        "SS" or "SSS" => 0.60m,
        _ => 1.00m,
    };

    /// US-154: aplica diminishing returns (rank multiplier + limite mensal) ao ganho bruto de RankScore.
    private RankScoreGainAudit ApplyRankScoreGain(int rawGain, decimal externalMultiplier, DateTime utcNow)
    {
        var yearMonth = utcNow.Year * 100 + utcNow.Month;
        if (MonthlyRankScoreResetYearMonth != yearMonth)
        {
            MonthlyRankScoreGain = 0;
            MonthlyRankScoreResetYearMonth = yearMonth;
        }

        var rankMultiplier = GetRankMultiplier(Rank);
        var afterExternal = (int)Math.Floor(rawGain * externalMultiplier);
        var afterDR = (int)Math.Floor(afterExternal * rankMultiplier);
        var monthlyRemaining = Math.Max(0, MonthlyRankScoreLimit - MonthlyRankScoreGain);
        var effectiveGain = Math.Min(afterDR, monthlyRemaining);
        var wasMonthlyLimitApplied = afterDR > 0 && effectiveGain < afterDR;

        RankScore += effectiveGain;
        MonthlyRankScoreGain += effectiveGain;
        Rank = RankCalculator.CalculateRank(RankScore);
        UpdatedAtUtc = utcNow;

        return new RankScoreGainAudit(rawGain, rankMultiplier, externalMultiplier, effectiveGain, wasMonthlyLimitApplied);
    }
}

/// US-130: resultado de uma chamada a AddAttributeXp — quantos level-ups ocorreram por atributo.
public record AttributeLevelUpsResult(
    int Strength,
    int Agility,
    int Endurance,
    int Vitality,
    int Focus,
    int Wisdom)
{
    public bool Any => Strength > 0 || Agility > 0 || Endurance > 0
        || Vitality > 0 || Focus > 0 || Wisdom > 0;

    public IReadOnlyList<string> ToNameList()
    {
        var list = new List<string>(6);
        if (Strength > 0) list.Add("strength");
        if (Agility > 0) list.Add("agility");
        if (Endurance > 0) list.Add("endurance");
        if (Vitality > 0) list.Add("vitality");
        if (Focus > 0) list.Add("focus");
        if (Wisdom > 0) list.Add("wisdom");
        return list;
    }
}

/// US-154: auditoria de um ganho de RankScore após aplicação de diminishing returns.
public record RankScoreGainAudit(
    int RawGain,
    decimal Multiplier,
    decimal ExternalMultiplier,
    int EffectiveGain,
    bool WasMonthlyLimitApplied);

/// US-154/US-155: resultado combinado de AddAttributeXp.
public record AddAttributeXpResult(
    AttributeLevelUpsResult LevelUps,
    RankScoreGainAudit RankScoreAudit);

/// US-154: resultado de TryApplyStreakMilestoneBonus com auditoria DR.
public record StreakMilestoneBonusResult(
    int RawBonus,
    RankScoreGainAudit? RankScoreAudit);
