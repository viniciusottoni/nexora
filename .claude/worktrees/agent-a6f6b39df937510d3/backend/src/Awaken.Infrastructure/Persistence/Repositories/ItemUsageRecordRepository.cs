using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

/// <summary>
/// US-230: implementação EF Core do repositório de registros de uso de itens.
/// </summary>
public class ItemUsageRecordRepository(AwakenDbContext context) : IItemUsageRecordRepository
{
    public async Task<ItemUsageRecord?> GetAsync(
        Guid userId,
        string itemKey,
        DateTime periodStart,
        CancellationToken cancellationToken = default)
        => await context.ItemUsageRecords
            .FirstOrDefaultAsync(
                r => r.UserId == userId && r.ItemKey == itemKey && r.PeriodStartUtc == periodStart,
                cancellationToken);

    public async Task AddAsync(ItemUsageRecord record, CancellationToken cancellationToken = default)
        => await context.ItemUsageRecords.AddAsync(record, cancellationToken);

    public void Update(ItemUsageRecord record)
        => context.ItemUsageRecords.Update(record);
}
