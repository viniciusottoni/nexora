namespace Awaken.Contracts.Admin.Subscriptions;

/// <summary>
/// US-217: detalhe seguro de uma validação de assinatura/IAP.
/// RN-004: payload bruto de provider nunca é exposto — apenas hash truncado e
/// referência externa mascarada (últimos 4 caracteres), seguindo o padrão de
/// RevenueCatEvent.PayloadHash (ADR-015).
/// </summary>
public record SubscriptionEventDetailResponse(
    Guid Id,
    string Source,
    string Type,
    string Store,
    string Status,
    string? Plan,
    string? Product,
    string Environment,
    Guid? UserId,
    string? MaskedExternalRef,
    string? PayloadHashMasked,
    bool IsRepeatedTransaction,
    bool IsPendingTooLong,
    string CreatedAtUtc,
    IReadOnlyList<SubscriptionEventSummaryResponse> RelatedUserEvents);
