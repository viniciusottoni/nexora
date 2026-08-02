using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Shop;

public class IapTransactionLedger : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TransactionId { get; private set; } = string.Empty; // unique per store transaction
    public string ProductKey { get; private set; } = string.Empty;
    public string Store { get; private set; } = "google_play"; // "google_play" | "app_store"
    public string Status { get; private set; } = "pending"; // "pending" | "granted" | "failed"
    public DateTime? GrantedAtUtc { get; private set; }

    private IapTransactionLedger() { }

    public static IapTransactionLedger Create(
        Guid userId, string transactionId, string productKey, string store, DateTime utcNow)
    {
        return new IapTransactionLedger
        {
            UserId = userId,
            TransactionId = transactionId,
            ProductKey = productKey,
            Store = store,
            Status = "pending",
            CreatedAtUtc = utcNow,
        };
    }

    public void MarkGranted(DateTime utcNow)
    {
        Status = "granted";
        GrantedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void MarkFailed(DateTime utcNow)
    {
        Status = "failed";
        UpdatedAtUtc = utcNow;
    }
}
