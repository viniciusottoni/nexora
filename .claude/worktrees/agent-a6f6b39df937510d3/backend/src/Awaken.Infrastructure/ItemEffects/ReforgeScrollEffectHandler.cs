using Awaken.Application.Quests.Common;
using Awaken.Domain.Entities.Inventory;
using Microsoft.Extensions.DependencyInjection;

namespace Awaken.Infrastructure.ItemEffects;

/// <summary>
/// US-230: handler do Pergaminho da Reforja (<see cref="ItemKeys.ReforgeScroll"/>).
/// Limite diário: 1 uso. Delega a mecânica de regeneração ao mesmo
/// <see cref="IQuestRegenerationService"/> usado por
/// RegenerateDailyQuestCommandHandler — antes deste fix, este handler era um
/// stub desconectado: o item era consumido sem regenerar nada.
/// </summary>
public class ReforgeScrollEffectHandler : IItemEffectHandler
{
    public string ItemKey => ItemKeys.ReforgeScroll;
    public bool ConsumesOnUse => true;
    public int UsageLimit => 1;
    public ItemUsageLimitPeriod LimitPeriod => ItemUsageLimitPeriod.Daily;

    public async Task<ItemEffectResult> ApplyAsync(UseItemContext context, CancellationToken ct)
    {
        var regenerationService = context.Services.GetRequiredService<IQuestRegenerationService>();
        await regenerationService.RegenerateAsync(context.UserId, viaReforgeScroll: true, ct);
        return new ItemEffectResult(true, "quest_regenerated", "Quest regenerada com sucesso.");
    }
}
