using System.Reflection;
using Awaken.Domain.Entities.Inventory;
using FluentAssertions;

namespace Awaken.UnitTests.Shop;

/// <summary>
/// US-229: verifica que todos os ItemKeys declarados possuem entrada
/// correspondente no ItemCatalog (CA-002: novo item = apenas dado, sem schema).
/// Tambem valida que os valores das constantes de ItemKeys batem com os
/// usados na migration SeedInitialShopCatalog.
/// </summary>
public class ShopCatalogSeedTests
{
    // ── Mapeamento chave → valor esperado ────────────────────────────────────

    [Theory]
    // Legado
    [InlineData(nameof(ItemKeys.ReforgeScroll),  "reforja_scroll")]
    [InlineData(nameof(ItemKeys.DungeonStone),   "pedra_dungeon")]
    // Consumiveis (US-229)
    [InlineData(nameof(ItemKeys.SubstitutionScroll), "scroll_substitution")]
    [InlineData(nameof(ItemKeys.DungeonCompass),     "dungeon_compass")]
    [InlineData(nameof(ItemKeys.DungeonKey),         "dungeon_key")]
    [InlineData(nameof(ItemKeys.ProtectionSeal),     "protection_seal")]
    [InlineData(nameof(ItemKeys.RecoveryTonic),      "recovery_tonic")]
    [InlineData(nameof(ItemKeys.ReturnAmulet),       "return_amulet")]
    [InlineData(nameof(ItemKeys.FocusPotion),        "focus_potion")]
    [InlineData(nameof(ItemKeys.FocusPotionLarge),   "focus_potion_large")]
    [InlineData(nameof(ItemKeys.LuckPotion),         "luck_potion")]
    // Cosmeticos — Molduras (US-229)
    [InlineData(nameof(ItemKeys.FrameRankE),   "frame_rank_e")]
    [InlineData(nameof(ItemKeys.FrameRankD),   "frame_rank_d")]
    [InlineData(nameof(ItemKeys.FrameRankC),   "frame_rank_c")]
    [InlineData(nameof(ItemKeys.FrameRankB),   "frame_rank_b")]
    [InlineData(nameof(ItemKeys.FrameRankA),   "frame_rank_a")]
    [InlineData(nameof(ItemKeys.FrameRankS),   "frame_rank_s")]
    [InlineData(nameof(ItemKeys.FrameRankSs),  "frame_rank_ss")]
    [InlineData(nameof(ItemKeys.FrameRankSss), "frame_rank_sss")]
    // Cosmeticos — Auras / Fundos (US-229)
    [InlineData(nameof(ItemKeys.AuraDefault),             "aura_default")]
    [InlineData(nameof(ItemKeys.BackgroundPortal),        "background_portal")]
    [InlineData(nameof(ItemKeys.BackgroundDungeon),       "background_dungeon")]
    [InlineData(nameof(ItemKeys.BackgroundHunterShadows), "background_hunter_shadows")]
    // Cosmeticos — Pergaminhos (US-229)
    [InlineData(nameof(ItemKeys.ScrollRename),      "scroll_rename")]
    [InlineData(nameof(ItemKeys.ScrollClassChange), "scroll_class_change")]
    // Packs (US-229)
    [InlineData(nameof(ItemKeys.PackStriker),    "pack_striker")]
    [InlineData(nameof(ItemKeys.PackRunner),     "pack_runner")]
    [InlineData(nameof(ItemKeys.PackGuardian),   "pack_guardian")]
    [InlineData(nameof(ItemKeys.PackShadow),     "pack_shadow")]
    [InlineData(nameof(ItemKeys.PackReawakened), "pack_reawakened")]
    public void ItemKeys_ConstantValue_MatchesMigrationKey(string constantName, string expectedValue)
    {
        var field = typeof(ItemKeys).GetField(constantName,
            BindingFlags.Public | BindingFlags.Static);

        field.Should().NotBeNull($"ItemKeys.{constantName} deve existir");

        var actualValue = (string)field!.GetValue(null)!;
        actualValue.Should().Be(expectedValue,
            $"ItemKeys.{constantName} deve ter o valor exato usado na migration");
    }

    // ── Todo ItemKeys declarado deve ter entrada no ItemCatalog ─────────────

    [Fact]
    public void AllDeclaredItemKeys_HaveCatalogEntry()
    {
        // Coleta todos os campos publicos constantes de ItemKeys via reflexao.
        var allKeys = typeof(ItemKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        allKeys.Should().NotBeEmpty("ItemKeys deve declarar pelo menos uma constante");

        var keysWithoutCatalogEntry = allKeys
            .Where(key => ItemCatalog.Find(key) is null)
            .ToList();

        keysWithoutCatalogEntry.Should().BeEmpty(
            "todo ItemKey declarado em ItemKeys deve ter uma entrada correspondente em ItemCatalog.Items " +
            "(CA-002: novo item = adicionar dado ao catalogo, sem migracao de schema)");
    }

    // ── Contagem total do catalogo (sanidade) ────────────────────────────────

    [Fact]
    public void ItemCatalog_ContainsExpectedTotalCount()
    {
        // 2 legado + 9 consumiveis + 14 cosmeticos + 5 packs = 30
        // (slots de inventario removidos pelo EPIC-021: RefineShopCatalogCategories
        // e CleanupShopCatalogOrphans excluiram esses produtos da loja).
        const int expectedCount = 30;

        ItemCatalog.Items.Should().HaveCount(expectedCount,
            "o catalogo deve conter exatamente {0} itens apos EPIC-021", expectedCount);
    }
}
