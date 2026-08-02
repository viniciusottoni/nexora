using Awaken.Contracts.Shop;
using MediatR;

namespace Awaken.Application.Shop.Queries.GetShopCatalog;

public record GetShopCatalogQuery : IRequest<IReadOnlyList<ShopProductResponse>>;
