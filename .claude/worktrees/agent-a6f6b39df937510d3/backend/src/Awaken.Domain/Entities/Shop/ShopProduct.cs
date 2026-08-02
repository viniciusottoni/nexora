using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Shop;

public class ShopProduct : BaseEntity
{
    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Type { get; private set; } = "consumable"; // "consumable" | "slot" | "cosmetic" | "pack"
    public string Rarity { get; private set; } = "common"; // "common" | "uncommon" | "rare" | "epic" | "legendary"
    public bool IsActive { get; private set; } = true;
    public string? RevenueCatProductId { get; private set; }
    public int? PriceGold { get; private set; }

    /// EPIC-021: frase temática (flavor) exibida como subtítulo do item na loja.
    /// Complementa a Description (efeito mecânico). Texto de catálogo (pt-BR).
    public string? Flavor { get; private set; }

    /// EPIC-021: regra de uso do item, como código semântico localizado no app
    /// ("daily_1", "daily_2", "weekly_1", "weekly_2", "monthly_1",
    /// "max_active_2", "one_time"). Null quando não se aplica.
    public string? UsageLimit { get; private set; }

    /// US-226: quantidade de Gold que este produto credita quando comprado via IAP
    /// (dinheiro real). Quando preenchido, o produto é um "pacote de Gold" — o app
    /// nunca informa a quantidade (RN-001/RN-002); o crédito é sempre resolvido por
    /// este valor server-side.
    public int? GoldAmount { get; private set; }

    /// Canal de venda do produto: "gold" quando há preço em Gold configurado,
    /// "iap" quando vinculado a um produto RevenueCat (RN-003).
    public string Channel => PriceGold.HasValue ? "gold" : "iap";

    private ShopProduct() { }

    public static ShopProduct Create(
        string key, string name, string? description,
        string type, string rarity, string? revenueCatProductId,
        DateTime utcNow, int? priceGold = null, int? goldAmount = null,
        string? flavor = null, string? usageLimit = null)
    {
        return new ShopProduct
        {
            Key = key,
            Name = name,
            Description = description,
            Type = type,
            Rarity = rarity,
            RevenueCatProductId = revenueCatProductId,
            PriceGold = priceGold,
            GoldAmount = goldAmount,
            Flavor = flavor,
            UsageLimit = usageLimit,
            IsActive = true,
            CreatedAtUtc = utcNow,
        };
    }

    public void Deactivate(DateTime utcNow) { IsActive = false; UpdatedAtUtc = utcNow; }
}
