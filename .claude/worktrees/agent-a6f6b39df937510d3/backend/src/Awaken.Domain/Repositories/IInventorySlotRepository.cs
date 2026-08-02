using Awaken.Domain.Common;
using Awaken.Domain.Entities.Inventory;

namespace Awaken.Domain.Repositories;

/// US-187: persistencia da estrutura de slot de inventario. Sem logica de
/// negocio associada ainda (ver InventorySlot).
public interface IInventorySlotRepository : IRepository<InventorySlot>
{
    Task<IEnumerable<InventorySlot>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<InventorySlot?> GetByUserIdAndSlotKeyAsync(
        Guid userId,
        string slotKey,
        CancellationToken cancellationToken = default);
}
