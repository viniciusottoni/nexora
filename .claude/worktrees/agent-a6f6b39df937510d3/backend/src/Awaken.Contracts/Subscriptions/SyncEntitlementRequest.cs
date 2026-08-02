namespace Awaken.Contracts.Subscriptions;

/// <summary>
/// US-194: the app can only send its RevenueCat customer ID so the backend
/// can link the record for webhook correlation.
/// The app is NOT allowed to send plan, expiry, or entitlement — those come
/// from the server-side webhook (ADR-009).
///
/// ClientPlan / ClientEntitlement / ClientExpiresAt are hints used ONLY when
/// RevenueCat:DevModeEnabled = true and server-side validation returns inactive.
/// Ignored in production.
/// </summary>
public record SyncEntitlementRequest(
    string RevenueCatCustomerId,
    string? OriginalRevenueCatCustomerId = null,
    string? Plan = null,
    string? Entitlement = null,
    DateTime? ExpiresAt = null);
