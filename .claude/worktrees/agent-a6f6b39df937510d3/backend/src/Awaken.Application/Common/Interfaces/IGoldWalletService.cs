using Awaken.Domain.Entities.Economy;

namespace Awaken.Application.Common.Interfaces;

/// US-186: operacoes transacionais de credito/debito de Gold, reutilizaveis
/// por compras (US-189) e auditoria (US-190). Cria a carteira do usuario de
/// forma preguicosa (saldo 0) caso ainda nao exista.
public interface IGoldWalletService
{
    /// Retorna o saldo do usuario, criando a carteira (saldo 0) se for o
    /// primeiro acesso (CA-001).
    Task<GoldWallet> GetOrCreateWalletAsync(Guid userId, CancellationToken cancellationToken = default);

    /// Credita Gold na carteira do usuario e grava o lancamento no ledger.
    Task<GoldLedgerEntry> CreditAsync(
        Guid userId,
        long amount,
        string reason,
        string? referenceType = null,
        string? referenceId = null,
        CancellationToken cancellationToken = default);

    /// Debita Gold da carteira do usuario e grava o lancamento no ledger.
    /// Lanca InsufficientGoldBalanceException sem gravar nada quando o
    /// saldo for insuficiente (RN-003/CA-002).
    Task<GoldLedgerEntry> DebitAsync(
        Guid userId,
        long amount,
        string reason,
        string? referenceType = null,
        string? referenceId = null,
        CancellationToken cancellationToken = default);
}
