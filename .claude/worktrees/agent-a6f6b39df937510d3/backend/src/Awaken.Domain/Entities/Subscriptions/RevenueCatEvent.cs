using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Subscriptions;

/// <summary>
/// US-194: idempotency ledger for RevenueCat server-side webhook events.
/// One record per unique EventId prevents duplicate processing.
/// PayloadHash stores a truncated SHA-256 of the raw body for audit — raw
/// payment data is never persisted (ADR-015).
/// </summary>
public class RevenueCatEvent : BaseEntity
{
    public string EventId { get; private set; } = null!;
    public string AppUserId { get; private set; } = null!;
    public string? OriginalTransactionId { get; private set; }
    public string? ProductId { get; private set; }
    public string Type { get; private set; } = null!;
    public DateTime ProcessedAtUtc { get; private set; }

    /// <summary>Truncated SHA-256 of the raw webhook payload (first 16 hex chars). No raw data stored.</summary>
    public string? PayloadHash { get; private set; }

    private RevenueCatEvent() { }

    public static RevenueCatEvent Create(
        string eventId,
        string appUserId,
        string type,
        DateTime processedAtUtc,
        string? originalTransactionId = null,
        string? productId = null,
        string? payloadHash = null)
    {
        return new RevenueCatEvent
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            AppUserId = appUserId,
            Type = type,
            ProcessedAtUtc = processedAtUtc,
            OriginalTransactionId = originalTransactionId,
            ProductId = productId,
            PayloadHash = payloadHash,
            CreatedAtUtc = processedAtUtc,
        };
    }
}
