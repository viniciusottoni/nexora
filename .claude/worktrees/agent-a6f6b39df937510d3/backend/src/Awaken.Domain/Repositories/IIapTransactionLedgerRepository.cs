using Awaken.Domain.Entities.Shop;

namespace Awaken.Domain.Repositories;

public interface IIapTransactionLedgerRepository
{
    Task<IapTransactionLedger?> GetByTransactionIdAsync(string transactionId, CancellationToken ct);
    Task AddAsync(IapTransactionLedger ledger, CancellationToken ct);
    void Update(IapTransactionLedger ledger);
}
