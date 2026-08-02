using Awaken.Domain.Common;
using Awaken.Domain.Entities.Inventory;

namespace Awaken.Domain.Repositories;

public interface IInventoryRepository : IRepository<InventoryItem>
{
    Task<InventoryItem?> GetByUserIdAndItemKeyAsync(
        Guid userId,
        string itemKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// US-227 / RN-006: recarrega o estado da entidade rastreada a partir do
    /// banco (incluindo Version/xmin) após um conflito de concorrência
    /// otimista, para que o retry de incremento opere sobre a quantidade atual.
    /// </summary>
    Task ReloadAsync(InventoryItem item, CancellationToken cancellationToken = default);
}
