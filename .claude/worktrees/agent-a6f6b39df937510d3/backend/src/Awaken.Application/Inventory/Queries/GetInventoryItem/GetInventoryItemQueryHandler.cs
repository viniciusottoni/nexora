using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Inventory;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Inventory.Queries.GetInventoryItem;

public class GetInventoryItemQueryHandler(
    IInventoryRepository inventoryRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetInventoryItemQuery, InventoryItemResponse>
{
    public async Task<InventoryItemResponse> Handle(
        GetInventoryItemQuery request, CancellationToken cancellationToken)
    {
        var item = await inventoryRepository.GetByUserIdAndItemKeyAsync(
            currentUserService.UserId, request.ItemKey, cancellationToken);

        return new InventoryItemResponse(request.ItemKey, item?.Quantity ?? 0);
    }
}
