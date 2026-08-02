using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class InventorySlotRepository(AwakenDbContext context) : IInventorySlotRepository
{
    public async Task<InventorySlot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.InventorySlots.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IEnumerable<InventorySlot>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.InventorySlots.ToListAsync(cancellationToken);

    public async Task AddAsync(InventorySlot entity, CancellationToken cancellationToken = default) =>
        await context.InventorySlots.AddAsync(entity, cancellationToken);

    public void Update(InventorySlot entity) => context.InventorySlots.Update(entity);

    public void Remove(InventorySlot entity) => context.InventorySlots.Remove(entity);

    public async Task<IEnumerable<InventorySlot>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await context.InventorySlots.Where(s => s.UserId == userId).ToListAsync(cancellationToken);

    public async Task<InventorySlot?> GetByUserIdAndSlotKeyAsync(
        Guid userId,
        string slotKey,
        CancellationToken cancellationToken = default) =>
        await context.InventorySlots.FirstOrDefaultAsync(
            s => s.UserId == userId && s.SlotKey == slotKey, cancellationToken);
}
