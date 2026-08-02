using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Inventory;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Shop.Commands.PurchaseShopItem;

public class PurchaseShopItemCommandHandler(
    IInventoryRepository inventoryRepository,
    IShopProductRepository shopProductRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork) : IRequestHandler<PurchaseShopItemCommand, InventoryItemResponse>
{
    public async Task<InventoryItemResponse> Handle(
        PurchaseShopItemCommand request, CancellationToken cancellationToken)
    {
        var product = await shopProductRepository.GetByKeyAsync(request.ItemKey, cancellationToken);
        if (product is null || !product.PriceGold.HasValue)
            throw new NotFoundException("ShopItem", request.ItemKey);

        var userId = currentUserService.UserId;
        var utcNow = dateTimeService.UtcNow;
        var item = await inventoryRepository.GetByUserIdAndItemKeyAsync(
            userId, request.ItemKey, cancellationToken);

        var maxQuantity = ItemCatalog.Find(request.ItemKey)?.MaxQuantity ?? 99;
        if ((item?.Quantity ?? 0) + 1 > maxQuantity)
            throw new ConflictException(
                "INVENTORY_MAX_QUANTITY",
                $"Quantidade maxima de {maxQuantity} atingida para o item '{request.ItemKey}'.");

        if (item is null)
        {
            item = InventoryItem.Create(userId, request.ItemKey);
            item.Add(1, utcNow);
            await inventoryRepository.AddAsync(item, cancellationToken);
        }
        else
        {
            item.Add(1, utcNow);
            inventoryRepository.Update(item);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new InventoryItemResponse(item.ItemKey, item.Quantity);
    }
}
