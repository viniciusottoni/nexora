using Awaken.Application.Common.Exceptions;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Awaken.Infrastructure.ItemEffects;

/// <summary>
/// US-230: abrir um Pack concede o bundle de consumíveis especificado no
/// catálogo da loja e a classe correspondente. O Pack em si NÃO é consumido
/// (ConsumesOnUse=false) — permanece na posse do usuário para preservar o
/// gate de avatares por posse (AvatarCatalog.RequiredItemKey). "Abrir" é
/// limitado a 1 vez na vida via UsageLimit=1 + LimitPeriod.Lifetime.
///
/// Nota: 3 dos itens do bundle (Poção da Sorte, Pedra da Dungeon, Bússola da
/// Dungeon) permanecem stub por decisão de escopo já tomada — são concedidos
/// normalmente, mas usá-los ainda não produz efeito real até a US de
/// dungeon/gold-por-quest.
/// </summary>
public abstract class PackEffectHandlerBase(string itemKey, string grantedClass) : IItemEffectHandler
{
    private static readonly IReadOnlyList<string> BundleItemKeys =
    [
        ItemKeys.ScrollRename,
        ItemKeys.ReforgeScroll,
        ItemKeys.SubstitutionScroll,
        ItemKeys.ProtectionSeal,
        ItemKeys.RecoveryTonic,
        ItemKeys.ReturnAmulet,
        ItemKeys.FocusPotion,
        ItemKeys.LuckPotion,
        ItemKeys.DungeonStone,
        ItemKeys.DungeonCompass,
    ];

    public string ItemKey => itemKey;
    public bool ConsumesOnUse => false;
    public int UsageLimit => 1;
    public ItemUsageLimitPeriod LimitPeriod => ItemUsageLimitPeriod.Lifetime;

    public async Task<ItemEffectResult> ApplyAsync(UseItemContext context, CancellationToken ct)
    {
        var inventoryRepository = context.Services.GetRequiredService<IInventoryRepository>();

        foreach (var bundleItemKey in BundleItemKeys)
        {
            var item = await inventoryRepository.GetByUserIdAndItemKeyAsync(context.UserId, bundleItemKey, ct);
            if (item is null)
            {
                item = InventoryItem.Create(context.UserId, bundleItemKey);
                await inventoryRepository.AddAsync(item, ct);
            }

            // Guard de MaxQuantity: InventoryItem.Add não aplica teto (só
            // IInventoryService.IncrementAsync aplica, e esse salva sozinho —
            // não pode ser usado aqui sem quebrar a atomicidade do único
            // SaveChangesAsync deste fluxo). Relevante p/ scroll_rename/
            // scroll_class_change (MaxQuantity=1): abrir 2 packs não deve
            // ultrapassar o teto do catálogo.
            var maxQuantity = ItemCatalog.Find(bundleItemKey)?.MaxQuantity ?? int.MaxValue;
            if (item.Quantity < maxQuantity)
                item.Add(1, context.UtcNow);
        }

        var progressionRepository = context.Services.GetRequiredService<IHunterProgressionRepository>();
        var progression = await progressionRepository.GetByUserIdAsync(context.UserId, ct)
            ?? throw new NotFoundException("HunterProgression", context.UserId);
        progression.ChangeClass(grantedClass, context.UtcNow);

        return new ItemEffectResult(true, "pack_opened");
    }
}

public class PackStrikerEffectHandler() : PackEffectHandlerBase(ItemKeys.PackStriker, "striker");
public class PackRunnerEffectHandler() : PackEffectHandlerBase(ItemKeys.PackRunner, "runner");
public class PackGuardianEffectHandler() : PackEffectHandlerBase(ItemKeys.PackGuardian, "guardian");
public class PackShadowEffectHandler() : PackEffectHandlerBase(ItemKeys.PackShadow, "shadow");
public class PackReawakenedEffectHandler() : PackEffectHandlerBase(ItemKeys.PackReawakened, "reawakened");
