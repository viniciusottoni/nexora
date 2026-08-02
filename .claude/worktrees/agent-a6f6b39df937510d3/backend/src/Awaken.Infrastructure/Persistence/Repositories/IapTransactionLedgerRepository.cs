using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class IapTransactionLedgerRepository(AwakenDbContext context) : IIapTransactionLedgerRepository
{
    public async Task<IapTransactionLedger?> GetByTransactionIdAsync(string transactionId, CancellationToken ct) =>
        await context.IapTransactionLedgers.FirstOrDefaultAsync(l => l.TransactionId == transactionId, ct);

    public async Task AddAsync(IapTransactionLedger ledger, CancellationToken ct) =>
        await context.IapTransactionLedgers.AddAsync(ledger, ct);

    public void Update(IapTransactionLedger ledger) =>
        context.IapTransactionLedgers.Update(ledger);
}
