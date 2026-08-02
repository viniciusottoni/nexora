namespace Awaken.Contracts.Shop;

public record ShopProductResponse(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    string Type,
    string Rarity,
    string? RevenueCatProductId,
    int? PriceGold,
    int? GoldAmount,
    string Channel,
    string? Flavor,
    string? UsageLimit);
