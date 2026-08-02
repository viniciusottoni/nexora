namespace Awaken.Contracts.Shop;

public record ProcessIapPurchaseRequest(
    string TransactionId,
    string ProductKey,
    string Store);
