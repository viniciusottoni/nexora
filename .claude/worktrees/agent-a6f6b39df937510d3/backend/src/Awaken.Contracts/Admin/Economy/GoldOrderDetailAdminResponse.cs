namespace Awaken.Contracts.Admin.Economy;

/// <summary>
/// US-229: detalhe de um ShopOrder com lançamentos de ledger relacionados.
/// RN-003: ExternalTransactionId mascarado/omitido.
/// </summary>
public record GoldOrderDetailAdminResponse(
    Guid Id,
    Guid UserId,
    string Channel,
    string ProductKey,
    string Status,
    string? CorrelationId,
    DateTime CreatedAtUtc,
    DateTime? GrantedAtUtc,
    DateTime? FailedAtUtc,
    IReadOnlyList<GoldLedgerEntryAdminResponse> RelatedLedger);
