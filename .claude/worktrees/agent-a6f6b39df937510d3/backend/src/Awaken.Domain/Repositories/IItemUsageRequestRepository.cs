using Awaken.Domain.Entities.Inventory;

namespace Awaken.Domain.Repositories;

/// <summary>
/// US-230 RN-003: repositório de registros de idempotência de uso de item.
/// </summary>
public interface IItemUsageRequestRepository
{
    Task<ItemUsageRequest?> GetByUseRequestIdAsync(
        string useRequestId,
        CancellationToken cancellationToken = default);

    Task AddAsync(ItemUsageRequest request, CancellationToken cancellationToken = default);
}
