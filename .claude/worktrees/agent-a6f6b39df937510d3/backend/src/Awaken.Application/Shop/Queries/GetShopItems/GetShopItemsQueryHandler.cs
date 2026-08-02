using Awaken.Contracts.Inventory;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Shop.Queries.GetShopItems;

public class GetShopItemsQueryHandler(IShopProductRepository shopProductRepository)
    : IRequestHandler<GetShopItemsQuery, IReadOnlyList<ShopItemResponse>>
{
    public async Task<IReadOnlyList<ShopItemResponse>> Handle(
        GetShopItemsQuery request, CancellationToken cancellationToken)
    {
        var products = await shopProductRepository.GetActiveProductsAsync(cancellationToken);

        return products
            .Where(p => p.PriceGold.HasValue)
            .Select(p => new ShopItemResponse(p.Key, p.PriceGold!.Value))
            .ToList();
    }
}
