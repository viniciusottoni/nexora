namespace Awaken.Domain.Entities.Inventory;

/// <summary>
/// US-230: chaves estáveis de EffectType usadas por ItemActiveEffect.
/// Compartilhadas entre os handlers que criam o efeito (Awaken.Infrastructure)
/// e os serviços/handlers que o consomem (Awaken.Application) — vivem no
/// domínio para não criar dependência de Application em Infrastructure.
/// </summary>
public static class ItemEffectTypes
{
    public const string StreakProtection = "streak_protection";
    public const string RecoveryDay = "recovery_day";
    public const string StreakRecovery = "streak_recovery";
    public const string XpBoost = "xp_boost";
}
