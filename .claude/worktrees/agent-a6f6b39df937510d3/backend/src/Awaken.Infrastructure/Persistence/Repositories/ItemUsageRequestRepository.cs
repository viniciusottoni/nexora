using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

/// <summary>
/// US-230 RN-003: implementação EF Core do repositório de idempotência de uso de item.
/// </summary>
public class ItemUsageRequestRepository(AwakenDbContext context) : IItemUsageRequestRepository
{
    public async Task<ItemUsageRequest?> GetByUseRequestIdAsync(
        string useRequestId,
        CancellationToken cancellationToken = default)
        => await context.ItemUsageRequests
            .FirstOrDefaultAsync(r => r.UseRequestId == useRequestId, cancellationToken);

    public async Task AddAsync(ItemUsageRequest request, CancellationToken cancellationToken = default)
        => await context.ItemUsageRequests.AddAsync(request, cancellationToken);
}
