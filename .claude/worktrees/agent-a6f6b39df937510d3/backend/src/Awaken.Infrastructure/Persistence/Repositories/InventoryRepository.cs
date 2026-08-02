using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class InventoryRepository(AwakenDbContext context) : IInventoryRepository
{
    public async Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.InventoryItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IEnumerable<InventoryItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.InventoryItems.ToListAsync(cancellationToken);

    public async Task AddAsync(InventoryItem entity, CancellationToken cancellationToken = default) =>
        await context.InventoryItems.AddAsync(entity, cancellationToken);

    public void Update(InventoryItem entity) => context.InventoryItems.Update(entity);

    public void Remove(InventoryItem entity) => context.InventoryItems.Remove(entity);

    public async Task<InventoryItem?> GetByUserIdAndItemKeyAsync(
        Guid userId,
        string itemKey,
        CancellationToken cancellationToken = default) =>
        await context.InventoryItems.FirstOrDefaultAsync(
            i => i.UserId == userId && i.ItemKey == itemKey, cancellationToken);

    public Task ReloadAsync(InventoryItem item, CancellationToken cancellationToken = default) =>
        context.Entry(item).ReloadAsync(cancellationToken);
}
