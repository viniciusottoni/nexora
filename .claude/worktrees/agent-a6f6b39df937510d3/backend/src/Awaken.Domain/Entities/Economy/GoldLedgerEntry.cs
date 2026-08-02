using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Economy;

/// US-186: lancamento imutavel (append-only) de movimentacao de Gold. Toda
/// movimentacao da GoldWallet gera um registro aqui, com o saldo resultante
/// (BalanceAfter), permitindo reconciliacao do saldo pela soma do ledger
/// (RN-004, ADR-023). Nao ha emissao de Gold nem custo de item nesta US.
public class GoldLedgerEntry : BaseEntity
{
    public Guid WalletId { get; private set; }
    public GoldLedgerDirection Direction { get; private set; }
    public long Amount { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }
    public long BalanceAfter { get; private set; }
    public string? CorrelationId { get; private set; }

    private GoldLedgerEntry() { }

    internal static GoldLedgerEntry Create(
        Guid walletId,
        GoldLedgerDirection direction,
        long amount,
        string reason,
        string? referenceType,
        string? referenceId,
        long balanceAfter,
        string? correlationId,
        DateTime utcNow) =>
        new()
        {
            WalletId = walletId,
            Direction = direction,
            Amount = amount,
            Reason = reason,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            BalanceAfter = balanceAfter,
            CorrelationId = correlationId,
            CreatedAtUtc = utcNow,
        };
}
