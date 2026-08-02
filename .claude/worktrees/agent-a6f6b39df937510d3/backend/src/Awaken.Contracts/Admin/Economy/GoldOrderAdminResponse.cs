namespace Awaken.Contracts.Admin.Economy;

/// <summary>
/// US-229: projeção de um ShopOrder (canal gold) para exibição admin.
/// RN-003: ExternalTransactionId é excluído (dado de provider/pagamento).
/// </summary>
public record GoldOrderAdminResponse(
    Guid Id,
    Guid UserId,
    string Channel,
    string ProductKey,
    string Status,
    string? CorrelationId,
    DateTime CreatedAtUtc,
    DateTime? GrantedAtUtc,
    DateTime? FailedAtUtc);
