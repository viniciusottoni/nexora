namespace Awaken.Contracts.Admin.Economy;

/// <summary>
/// US-229: projeção de um GoldLedgerEntry para exibição admin.
/// RN-003: sem dados sensíveis de pagamento.
/// </summary>
public record GoldLedgerEntryAdminResponse(
    Guid Id,
    Guid WalletId,
    Guid UserId,
    string Direction,
    long Amount,
    string Reason,
    string? ReferenceType,
    string? ReferenceId,
    long BalanceAfter,
    string? CorrelationId,
    DateTime CreatedAtUtc);
