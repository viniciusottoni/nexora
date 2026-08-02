namespace Awaken.Domain.Entities.Inventory;

/// <summary>
/// US-230 RN-005: efeito de item com validade e estado claro (buff pendente).
/// Cobre Selo de Proteção/Tônico de Recuperação ("streak_protection"/
/// "recovery_day"), Amuleto de Retorno ("streak_recovery") e Poção de
/// Foco/Grande ("xp_boost"). EffectDateUtc é o dia-alvo do efeito quando
/// aplicável (Tônico/Amuleto); ExpiresAtUtc é o horizonte de limpeza — nunca
/// usado sozinho para decidir se um efeito ainda protege um dia já passado
/// (isso deve comparar contra o dia sendo processado, não contra "agora").
/// </summary>
public class ItemActiveEffect
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string ItemKey { get; private set; } = string.Empty;
    public string EffectType { get; private set; } = string.Empty;
    public string Status { get; private set; } = "active"; // active | consumed | expired
    public decimal? XpBoostMultiplier { get; private set; }
    public int? StreakDaysToRestore { get; private set; }
    public DateTime? EffectDateUtc { get; private set; }
    public DateTime ActivatedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public DateTime? ConsumedAtUtc { get; private set; }

    /// US-227-like: token de concorrencia otimista (xmin), evita que duas
    /// requisicoes concorrentes consumam o mesmo efeito duas vezes.
    public uint Version { get; private set; }

    private ItemActiveEffect() { }

    public static ItemActiveEffect Create(
        Guid userId,
        string itemKey,
        string effectType,
        DateTime activatedAtUtc,
        DateTime? expiresAtUtc = null,
        decimal? xpBoostMultiplier = null,
        int? streakDaysToRestore = null,
        DateTime? effectDateUtc = null)
    {
        return new ItemActiveEffect
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ItemKey = itemKey,
            EffectType = effectType,
            Status = "active",
            XpBoostMultiplier = xpBoostMultiplier,
            StreakDaysToRestore = streakDaysToRestore,
            EffectDateUtc = effectDateUtc,
            ActivatedAtUtc = activatedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
        };
    }

    public bool IsActive => Status == "active";

    public void Consume(DateTime utcNow)
    {
        if (Status != "active")
            throw new InvalidOperationException("Efeito nao esta ativo.");

        Status = "consumed";
        ConsumedAtUtc = utcNow;
    }
}
