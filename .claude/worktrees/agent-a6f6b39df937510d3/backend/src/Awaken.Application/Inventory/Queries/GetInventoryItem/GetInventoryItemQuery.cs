using Awaken.Contracts.Inventory;
using MediatR;

namespace Awaken.Application.Inventory.Queries.GetInventoryItem;

public record GetInventoryItemQuery(string ItemKey) : IRequest<InventoryItemResponse>;
