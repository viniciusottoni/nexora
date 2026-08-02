namespace Awaken.Contracts.Admin.Subscriptions;

/// <summary>
/// US-217: linha de evento de assinatura/IAP na listagem paginada.
/// RN-004: nenhum payload bruto de provider é exposto — apenas referência mascarada.
/// </summary>
public record SubscriptionEventSummaryResponse(
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
    bool IsRepeatedTransaction,
    bool IsPendingTooLong,
    string CreatedAtUtc);
