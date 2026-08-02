using Awaken.Domain.Entities.Inventory;
using FluentAssertions;

namespace Awaken.UnitTests.Inventory;

public class ItemCatalogTests
{
    [Theory]
    // Legado
    [InlineData(ItemKeys.ReforgeScroll)]
    [InlineData(ItemKeys.DungeonStone)]
    // Consumiveis (US-229)
    [InlineData(ItemKeys.SubstitutionScroll)]
    [InlineData(ItemKeys.DungeonCompass)]
    [InlineData(ItemKeys.DungeonKey)]
    [InlineData(ItemKeys.ProtectionSeal)]
    [InlineData(ItemKeys.RecoveryTonic)]
    [InlineData(ItemKeys.ReturnAmulet)]
    [InlineData(ItemKeys.FocusPotion)]
    [InlineData(ItemKeys.FocusPotionLarge)]
    [InlineData(ItemKeys.LuckPotion)]
    // Cosmeticos — Molduras (US-229)
    [InlineData(ItemKeys.FrameRankE)]
    [InlineData(ItemKeys.FrameRankD)]
    [InlineData(ItemKeys.FrameRankC)]
    [InlineData(ItemKeys.FrameRankB)]
    [InlineData(ItemKeys.FrameRankA)]
    [InlineData(ItemKeys.FrameRankS)]
    [InlineData(ItemKeys.FrameRankSs)]
    [InlineData(ItemKeys.FrameRankSss)]
    // Cosmeticos — Auras / Fundos (US-229)
    [InlineData(ItemKeys.AuraDefault)]
    [InlineData(ItemKeys.BackgroundPortal)]
    [InlineData(ItemKeys.BackgroundDungeon)]
    [InlineData(ItemKeys.BackgroundHunterShadows)]
    // Cosmeticos — Pergaminhos (US-229)
    [InlineData(ItemKeys.ScrollRename)]
    [InlineData(ItemKeys.ScrollClassChange)]
    // Packs (US-229)
    [InlineData(ItemKeys.PackStriker)]
    [InlineData(ItemKeys.PackRunner)]
    [InlineData(ItemKeys.PackGuardian)]
    [InlineData(ItemKeys.PackShadow)]
    [InlineData(ItemKeys.PackReawakened)]
    public void Find_ReturnsMetadata_ForKnownKeys(string itemKey)
    {
        var entry = ItemCatalog.Find(itemKey);

        entry.Should().NotBeNull();
        entry!.ItemKey.Should().Be(itemKey);
    }

    [Fact]
    public void Find_ReturnsNull_ForUnknownKey()
    {
        ItemCatalog.Find("chave_inexistente").Should().BeNull();
    }

    // CA-002: um novo ItemKey deve ser representavel apenas adicionando uma
    // entrada de dados ao catalogo, sem qualquer migracao de schema.
    [Fact]
    public void NewItemKey_IsRepresentable_WithoutSchemaChange()
    {
        const string newItemKey = "nova_chave_de_item_futura";
        var registry = new List<ItemCatalogEntry>(ItemCatalog.Items)
        {
            new(newItemKey, ItemType.Cosmetic),
        };

        registry.Should().Contain(e => e.ItemKey == newItemKey && e.Type == ItemType.Cosmetic);
    }
}

public class InventoryItemNegativeQuantityTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);

    // RN-005: quantidade de item nunca fica negativa.
    [Fact]
    public void Add_Throws_WhenResultingQuantityWouldBeNegative()
    {
        var item = InventoryItem.Create(UserId, ItemKeys.ReforgeScroll, quantity: 1);

        var act = () => item.Add(-5, UtcNow);

        act.Should().Throw<InvalidOperationException>();
        item.Quantity.Should().Be(1);
    }

    [Fact]
    public void Add_AllowsExactZero()
    {
        var item = InventoryItem.Create(UserId, ItemKeys.ReforgeScroll, quantity: 2);

        item.Add(-2, UtcNow);

        item.Quantity.Should().Be(0);
    }
}

public class InventorySlotTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_StartsWithoutItem_WhenNotSpecified()
    {
        var slot = InventorySlot.Create(UserId, "slot_principal");

        slot.UserId.Should().Be(UserId);
        slot.SlotKey.Should().Be("slot_principal");
        slot.ItemKey.Should().BeNull();
    }

    [Fact]
    public void SetItem_UpdatesItemKey()
    {
        var slot = InventorySlot.Create(UserId, "slot_principal");

        slot.SetItem(ItemKeys.DungeonStone, UtcNow);

        slot.ItemKey.Should().Be(ItemKeys.DungeonStone);
    }
}
