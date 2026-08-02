namespace Awaken.Contracts.Inventory;

public record InventoryItemResponse(string ItemKey, int Quantity);

public record ShopItemResponse(string ItemKey, int PriceGold);
