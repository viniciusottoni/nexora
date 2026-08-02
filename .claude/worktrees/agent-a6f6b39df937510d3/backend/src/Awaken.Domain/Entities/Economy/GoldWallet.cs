using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Economy;

/// US-186: contemner de saldo de Gold do usuario, unico por UserId. O backend
/// e a unica autoridade do saldo (RN-001, ADR-009/ADR-023); o saldo so muda
/// via Credit/Debit, que sempre geram um GoldLedgerEntry imutavel com o saldo
/// resultante (RN-002). Esta US nao define como o Gold e emitido nem o que
/// ele compra - apenas a estrutura que guarda e movimenta saldo.
public class GoldWallet : BaseEntity
{
    public Guid UserId { get; private set; }
    public long Balance { get; private set; }

    /// Token de concorrencia otimista (RN-005): mapeado para a coluna de
    /// sistema "xmin" do PostgreSQL pela configuracao EF Core, evitando que
    /// movimentacoes concorrentes corrompam o saldo.
    public uint Version { get; private set; }

    private GoldWallet() { }

    public static GoldWallet CreateEmpty(Guid userId, DateTime utcNow) =>
        new()
        {
            UserId = userId,
            Balance = 0,
            CreatedAtUtc = utcNow,
        };

    /// Credita Gold na carteira e retorna o lancamento de ledger gerado.
    /// Nao ha limite de credito nesta US (regras de emissao ficam fora de
    /// escopo - ver ADR-023).
    public GoldLedgerEntry Credit(
        long amount,
        string reason,
        string? referenceType,
        string? referenceId,
        string? correlationId,
        DateTime utcNow)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "O valor creditado deve ser positivo.");

        Balance += amount;
        UpdatedAtUtc = utcNow;

        return GoldLedgerEntry.Create(
            Id, GoldLedgerDirection.Credit, amount, reason, referenceType, referenceId, Balance, correlationId, utcNow);
    }

    /// Debita Gold da carteira e retorna o lancamento de ledger gerado.
    /// Lanca InsufficientGoldBalanceException sem alterar o saldo nem gerar
    /// lancamento quando o saldo for insuficiente (RN-003/CA-002).
    public GoldLedgerEntry Debit(
        long amount,
        string reason,
        string? referenceType,
        string? referenceId,
        string? correlationId,
        DateTime utcNow)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "O valor debitado deve ser positivo.");

        if (amount > Balance)
            throw new InsufficientGoldBalanceException(UserId, Balance, amount);

        Balance -= amount;
        UpdatedAtUtc = utcNow;

        return GoldLedgerEntry.Create(
            Id, GoldLedgerDirection.Debit, amount, reason, referenceType, referenceId, Balance, correlationId, utcNow);
    }
}
