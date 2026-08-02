using Awaken.Domain.Entities.Inventory;

namespace Awaken.Infrastructure.ItemEffects;

/// <summary>
/// US-230: handler padrão/fallback para itens sem um handler específico
/// (cosméticos, packs, slots). Consome o item e retorna "item_applied".
/// Não é registrado pelo ItemKey — é injetado diretamente como
/// <see cref="DefaultItemEffectHandler"/> e usado pelo handler principal
/// quando nenhum handler específico é encontrado.
/// </summary>
public class DefaultItemEffectHandler : IItemEffectHandler
{
    public string ItemKey => "*";
    public bool ConsumesOnUse => true;
    public int UsageLimit => 0;
    public ItemUsageLimitPeriod LimitPeriod => ItemUsageLimitPeriod.Unlimited;

    public Task<ItemEffectResult> ApplyAsync(UseItemContext context, CancellationToken ct)
        => Task.FromResult(new ItemEffectResult(true, "item_applied"));
}
