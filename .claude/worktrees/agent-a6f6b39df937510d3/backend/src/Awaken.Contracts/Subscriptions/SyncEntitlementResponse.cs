namespace Awaken.Contracts.Subscriptions;

/// <summary>
/// US-194: server-authoritative subscription status returned to the app after sync.
/// The app must use this response — it must never maintain its own subscription state.
/// </summary>
public record SyncEntitlementResponse(
    string AccessStatus,
    string? Plan,
    DateTime? ExpiresAt,
    bool AccessRestored);
