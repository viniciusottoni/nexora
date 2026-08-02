using Awaken.Contracts.Shop;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Shop.Queries.GetShopCatalog;

public class GetShopCatalogQueryHandler(IShopProductRepository shopProductRepository)
    : IRequestHandler<GetShopCatalogQuery, IReadOnlyList<ShopProductResponse>>
{
    public async Task<IReadOnlyList<ShopProductResponse>> Handle(
        GetShopCatalogQuery request, CancellationToken cancellationToken)
    {
        var products = await shopProductRepository.GetActiveProductsAsync(cancellationToken);

        return products
            .Select(p => new ShopProductResponse(
                p.Id,
                p.Key,
                p.Name,
                p.Description,
                p.Type,
                p.Rarity,
                p.RevenueCatProductId,
                p.PriceGold,
                p.GoldAmount,
                p.Channel,
                p.Flavor,
                p.UsageLimit))
            .ToList();
    }
}
