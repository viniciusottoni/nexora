using Awaken.Contracts.Inventory;
using MediatR;

namespace Awaken.Application.Shop.Queries.GetShopItems;

public record GetShopItemsQuery : IRequest<IReadOnlyList<ShopItemResponse>>;
