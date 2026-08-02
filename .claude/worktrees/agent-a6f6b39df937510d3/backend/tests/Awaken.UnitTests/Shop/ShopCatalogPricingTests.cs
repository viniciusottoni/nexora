using System.Reflection;
using Awaken.Domain.Entities.Inventory;
using FluentAssertions;

namespace Awaken.UnitTests.Shop;

/// <summary>
/// EPIC-021: fixa (pinning test) preco em Gold, raridade e regra de uso dos
/// itens finais da loja, exatamente como definidos em
/// RefineShopCatalogCategories (a migration mais recente que reescreve o
/// catalogo completo de "shop_products" via UPSERT; CleanupShopCatalogOrphans
/// apenas remove linhas fora do catalogo oficial, sem alterar valores).
///
/// O dominio nao mantem um catalogo estatico de precos/raridade em codigo
/// (ver NoStaticShopCatalogTests: preco/raridade sao dados de
/// "shop_products", nunca constantes de codigo) - por isso, assim como
/// ShopCatalogSeedTests faz para as chaves, este teste hardcoda os valores
/// esperados (transcritos das migrations) em vez de reimplementar um parser
/// de SQL. Cada linha e ancorada em ItemKeys/ItemCatalog para garantir que a
/// chave precificada continua existindo e classificada com o tipo correto.
///
/// Motivacao (auditoria EPIC-021): o seed inicial (SeedInitialShopCatalog)
/// chegou a conter 3 raridades erradas, corrigidas depois por
/// RefineShopCatalogCategories, sem que nenhum teste tivesse detectado o
/// problema. Este teste existe para que um erro semelhante em uma migration
/// futura exija atualizar conscientemente estes valores.
/// </summary>
public class ShopCatalogPricingTests
{
    private const string Consumable = "consumable";
    private const string Cosmetic = "cosmetic";
    private const string Profile = "profile";
    private const string Pack = "pack";

    public static IEnumerable<object?[]> Catalog()
    {
        // key, categoria (Type em shop_products), preco em Gold, raridade, regra de uso (UsageLimit)

        // ── Consumiveis ──────────────────────────────────────────────────────
        yield return [ItemKeys.ReforgeScroll, Consumable, 150, "uncommon", "daily_1"];
        yield return [ItemKeys.SubstitutionScroll, Consumable, 90, "common", "daily_2"];
        yield return [ItemKeys.DungeonCompass, Consumable, 120, "uncommon", "daily_1"];
        yield return [ItemKeys.DungeonKey, Consumable, 250, "epic", "daily_1"];
        yield return [ItemKeys.ProtectionSeal, Consumable, 100, "uncommon", "max_active_2"];
        yield return [ItemKeys.RecoveryTonic, Consumable, 70, "common", "weekly_2"];
        yield return [ItemKeys.ReturnAmulet, Consumable, 220, "epic", "weekly_1"];
        yield return [ItemKeys.FocusPotion, Consumable, 120, "uncommon", "daily_1"];
        yield return [ItemKeys.FocusPotionLarge, Consumable, 260, "epic", "weekly_1"];
        yield return [ItemKeys.LuckPotion, Consumable, 90, "common", "daily_1"];
        yield return [ItemKeys.DungeonStone, Consumable, 120, "uncommon", "daily_1"];

        // ── Cosmeticos — Molduras ────────────────────────────────────────────
        yield return [ItemKeys.FrameRankE, Cosmetic, 250, "common", "one_time"];
        yield return [ItemKeys.FrameRankD, Cosmetic, 350, "common", "one_time"];
        yield return [ItemKeys.FrameRankC, Cosmetic, 500, "uncommon", "one_time"];
        yield return [ItemKeys.FrameRankB, Cosmetic, 750, "rare", "one_time"];
        yield return [ItemKeys.FrameRankA, Cosmetic, 1000, "rare", "one_time"];
        yield return [ItemKeys.FrameRankS, Cosmetic, 1500, "epic", "one_time"];
        yield return [ItemKeys.FrameRankSs, Cosmetic, 2000, "epic", "one_time"];
        yield return [ItemKeys.FrameRankSss, Cosmetic, 3000, "legendary", "one_time"];

        // ── Cosmeticos — Auras / Fundos ──────────────────────────────────────
        yield return [ItemKeys.AuraDefault, Cosmetic, 600, "epic", "one_time"];
        yield return [ItemKeys.BackgroundPortal, Cosmetic, 450, "uncommon", "one_time"];
        yield return [ItemKeys.BackgroundDungeon, Cosmetic, 600, "rare", "one_time"];
        yield return [ItemKeys.BackgroundHunterShadows, Cosmetic, 900, "epic", "one_time"];

        // ── Perfil (movidos de "cosmetic" para "profile" pelo EPIC-021) ─────
        yield return [ItemKeys.ScrollRename, Profile, 200, "common", "monthly_1"];
        yield return [ItemKeys.ScrollClassChange, Profile, 350, "uncommon", "monthly_1"];

        // ── Packs ────────────────────────────────────────────────────────────
        yield return [ItemKeys.PackStriker, Pack, 1500, "uncommon", "one_time"];
        yield return [ItemKeys.PackRunner, Pack, 1500, "uncommon", "one_time"];
        yield return [ItemKeys.PackGuardian, Pack, 1500, "uncommon", "one_time"];
        yield return [ItemKeys.PackShadow, Pack, 1800, "rare", "one_time"];
        yield return [ItemKeys.PackReawakened, Pack, 2000, "rare", "one_time"];
    }

    [Theory]
    [MemberData(nameof(Catalog))]
    public void ShopItem_HasExpectedPriceRarityAndUsageLimit(
        string itemKey, string category, int expectedPriceGold, string expectedRarity, string? expectedUsageLimit)
    {
        // Sanidade dos proprios dados transcritos da migration.
        expectedPriceGold.Should().BeGreaterThan(0, $"'{itemKey}' deve ter preco positivo em Gold");
        expectedRarity.Should().BeOneOf(["common", "uncommon", "rare", "epic", "legendary"],
            $"'{itemKey}' deve usar uma raridade valida");
        if (expectedUsageLimit is not null)
        {
            expectedUsageLimit.Should().BeOneOf(
                ["daily_1", "daily_2", "weekly_1", "weekly_2", "monthly_1", "max_active_2", "one_time"],
                $"'{itemKey}' deve usar um codigo de UsageLimit valido (ShopProduct.UsageLimit)");
        }

        // Ancora 1: a chave precificada deve continuar declarada em ItemKeys.
        var keyField = typeof(ItemKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .FirstOrDefault(f => (string)f.GetValue(null)! == itemKey);
        keyField.Should().NotBeNull(
            $"o item de loja '{itemKey}' (preco {expectedPriceGold} Gold, raridade {expectedRarity}) " +
            "deve ter uma constante correspondente em ItemKeys");

        // Ancora 2: a chave deve continuar catalogada em ItemCatalog com o tipo compativel
        // (Packs e itens de Perfil sao tratados como Cosmetic ate a criacao de ItemType.Pack).
        var entry = ItemCatalog.Find(itemKey);
        entry.Should().NotBeNull($"o item de loja '{itemKey}' deve ter entrada correspondente em ItemCatalog");

        var expectedType = category == Consumable ? ItemType.Consumable : ItemType.Cosmetic;
        entry!.Type.Should().Be(expectedType,
            $"'{itemKey}' e do tipo '{category}' em shop_products (EPIC-021) e deve estar catalogado como {expectedType}");
    }

    [Fact]
    public void Catalog_CoversExactlyTheItemsWithGoldPricing()
    {
        // Sanidade: cobre todos os itens de loja com preco em Gold (exclui os
        // pacotes de Gold "gold_pack_*", que sao vendidos via IAP - RN-003).
        Catalog().Should().HaveCount(30);
    }
}
