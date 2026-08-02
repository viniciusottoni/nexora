namespace Awaken.Domain.Entities.Inventory;

/// <summary>
/// US-230: define a janela temporal para contagem de uso de itens consumíveis.
/// </summary>
public enum ItemUsageLimitPeriod
{
    Daily,
    Weekly,
    Unlimited,

    /// US-230: limite vitalício (nunca reseta) — usado por itens de uso único
    /// na vida do usuário, como abrir um Pack (UsageLimit=1).
    Lifetime
}
