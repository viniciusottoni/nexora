namespace Awaken.Contracts.Admin.Subscriptions;

/// <summary>
/// US-217: cards agregados de validações de assinatura/IAP.
/// RN-001: o backend é a fonte de verdade — estes números refletem exatamente o
/// que foi processado pelos webhooks/validações server-side (US-194/US-195).
/// </summary>
public record SubscriptionDiagnosticsResponse(
    int ApprovedCount,
    int DeniedCount,
    int PendingCount,
    int FailedCount,
    int RepeatedTransactionsCount,
    int PendingGrantsCount);
