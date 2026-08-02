namespace Awaken.Contracts.Admin.Economy;

/// <summary>
/// US-229: detalhe seguro de uma GoldWallet + lançamentos recentes.
/// RN-003: sem dados sensíveis; apenas saldo atual e ledger.
/// </summary>
public record GoldWalletAdminResponse(
    Guid Id,
    Guid UserId,
    long Balance,
    DateTime CreatedAtUtc,
    IReadOnlyList<GoldLedgerEntryAdminResponse> RecentLedger,
    int TotalLedgerEntries);
