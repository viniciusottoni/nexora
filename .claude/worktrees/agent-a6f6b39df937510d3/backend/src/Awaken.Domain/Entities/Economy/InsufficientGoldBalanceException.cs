namespace Awaken.Domain.Entities.Economy;

/// RN-003/CA-002: lancada quando um debito resultaria em saldo negativo.
/// Nenhum lancamento de ledger e gravado quando esta excecao e lancada.
public class InsufficientGoldBalanceException(Guid userId, long currentBalance, long requestedAmount)
    : Exception($"Saldo insuficiente para o usuario '{userId}': saldo atual {currentBalance}, valor solicitado {requestedAmount}.")
{
    public Guid UserId { get; } = userId;
    public long CurrentBalance { get; } = currentBalance;
    public long RequestedAmount { get; } = requestedAmount;
}
