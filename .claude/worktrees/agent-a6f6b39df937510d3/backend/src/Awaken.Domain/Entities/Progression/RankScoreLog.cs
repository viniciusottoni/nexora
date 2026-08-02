using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Progression;

/// US-154/US-155: registro de auditoria de cada alteração de RankScore.
public class RankScoreLog : BaseEntity
{
    public Guid UserId { get; private set; }

    /// Origem do ganho: "quest_exercise" ou "streak_bonus".
    public string Source { get; private set; } = string.Empty;

    public int RawGain { get; private set; }

    /// Multiplicador de diminishing returns (rank-based).
    public decimal Multiplier { get; private set; }

    /// Multiplicador externo aplicado pela camada de aplicação (ex: anti-abuso).
    public decimal ExternalMultiplier { get; private set; }

    public int EffectiveGain { get; private set; }
    public bool WasMonthlyLimitApplied { get; private set; }

    /// US-155: true quando padrão de abuso foi detectado.
    public bool WasAbuseSuspected { get; private set; }

    public int RankScoreAfter { get; private set; }

    private RankScoreLog() { }

    public static RankScoreLog Create(
        Guid userId,
        string source,
        int rawGain,
        decimal multiplier,
        decimal externalMultiplier,
        int effectiveGain,
        bool wasMonthlyLimitApplied,
        bool wasAbuseSuspected,
        int rankScoreAfter) =>
        new()
        {
            UserId = userId,
            Source = source,
            RawGain = rawGain,
            Multiplier = multiplier,
            ExternalMultiplier = externalMultiplier,
            EffectiveGain = effectiveGain,
            WasMonthlyLimitApplied = wasMonthlyLimitApplied,
            WasAbuseSuspected = wasAbuseSuspected,
            RankScoreAfter = rankScoreAfter,
        };
}
