using Awaken.Domain.Entities.Inventory;

namespace Awaken.Domain.Repositories;

/// <summary>
/// US-230: repositório de registros de uso de itens por período.
/// </summary>
public interface IItemUsageRecordRepository
{
    Task<ItemUsageRecord?> GetAsync(
        Guid userId,
        string itemKey,
        DateTime periodStart,
        CancellationToken cancellationToken = default);

    Task AddAsync(ItemUsageRecord record, CancellationToken cancellationToken = default);

    void Update(ItemUsageRecord record);
}
