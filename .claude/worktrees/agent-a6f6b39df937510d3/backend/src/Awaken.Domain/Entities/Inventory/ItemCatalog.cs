namespace Awaken.Domain.Entities.Inventory;

/// US-187: registro estatico de metadados por ItemKey (tipo do item). Novos
/// itens entram aqui apenas como dado, sem exigir migracao de schema
/// (RN-002/CA-002). Nao confundir com ShopCatalog (precos de loja, ADR-022) -
/// este catalogo descreve o item em si, independente de canal de aquisicao.
public static class ItemCatalog
{
    public static readonly IReadOnlyList<ItemCatalogEntry> Items =
    [
        // Legado (US-188)
        new ItemCatalogEntry(ItemKeys.ReforgeScroll, ItemType.Consumable),
        new ItemCatalogEntry(ItemKeys.DungeonStone,  ItemType.Consumable),

        // Consumiveis (US-229)
        new ItemCatalogEntry(ItemKeys.SubstitutionScroll, ItemType.Consumable),
        new ItemCatalogEntry(ItemKeys.DungeonCompass,     ItemType.Consumable),
        new ItemCatalogEntry(ItemKeys.DungeonKey,         ItemType.Consumable),
        new ItemCatalogEntry(ItemKeys.ProtectionSeal,     ItemType.Consumable),
        new ItemCatalogEntry(ItemKeys.RecoveryTonic,      ItemType.Consumable),
        new ItemCatalogEntry(ItemKeys.ReturnAmulet,       ItemType.Consumable),
        new ItemCatalogEntry(ItemKeys.FocusPotion,        ItemType.Consumable),
        new ItemCatalogEntry(ItemKeys.FocusPotionLarge,   ItemType.Consumable),
        new ItemCatalogEntry(ItemKeys.LuckPotion,         ItemType.Consumable),

        // Cosmeticos — Molduras (US-229) — max 1: nao pode duplicar cosmetico
        new ItemCatalogEntry(ItemKeys.FrameRankE,   ItemType.Cosmetic, MaxQuantity: 1),
        new ItemCatalogEntry(ItemKeys.FrameRankD,   ItemType.Cosmetic, MaxQuantity: 1),
        new ItemCatalogEntry(ItemKeys.FrameRankC,   ItemType.Cosmetic, MaxQuantity: 1),
        new ItemCatalogEntry(ItemKeys.FrameRankB,   ItemType.Cosmetic, MaxQuantity: 1),
        new ItemCatalogEntry(ItemKeys.FrameRankA,   ItemType.Cosmetic, MaxQuantity: 1),
        new ItemCatalogEntry(ItemKeys.FrameRankS,   ItemType.Cosmetic, MaxQuantity: 1),
        new ItemCatalogEntry(ItemKeys.FrameRankSs,  ItemType.Cosmetic, MaxQuantity: 1),
        new ItemCatalogEntry(ItemKeys.FrameRankSss, ItemType.Cosmetic, MaxQuantity: 1),

        // Cosmeticos — Auras / Fundos (US-229)
        new ItemCatalogEntry(ItemKeys.AuraDefault,             ItemType.Cosmetic, MaxQuantity: 1),
        new ItemCatalogEntry(ItemKeys.BackgroundPortal,        ItemType.Cosmetic, MaxQuantity: 1),
        new ItemCatalogEntry(ItemKeys.BackgroundDungeon,       ItemType.Cosmetic, MaxQuantity: 1),
        new ItemCatalogEntry(ItemKeys.BackgroundHunterShadows, ItemType.Cosmetic, MaxQuantity: 1),

        // Cosmeticos — Pergaminhos (US-229)
        new ItemCatalogEntry(ItemKeys.ScrollRename,      ItemType.Cosmetic, MaxQuantity: 1),
        new ItemCatalogEntry(ItemKeys.ScrollClassChange, ItemType.Cosmetic, MaxQuantity: 1),

        // Packs (US-229) — tratados como cosmeticos ate criacao de ItemType.Pack
        new ItemCatalogEntry(ItemKeys.PackStriker,    ItemType.Cosmetic, MaxQuantity: 1),
        new ItemCatalogEntry(ItemKeys.PackRunner,     ItemType.Cosmetic, MaxQuantity: 1),
        new ItemCatalogEntry(ItemKeys.PackGuardian,   ItemType.Cosmetic, MaxQuantity: 1),
        new ItemCatalogEntry(ItemKeys.PackShadow,     ItemType.Cosmetic, MaxQuantity: 1),
        new ItemCatalogEntry(ItemKeys.PackReawakened, ItemType.Cosmetic, MaxQuantity: 1),
    ];

    public static ItemCatalogEntry? Find(string itemKey) =>
        Items.FirstOrDefault(i => i.ItemKey == itemKey);
}
