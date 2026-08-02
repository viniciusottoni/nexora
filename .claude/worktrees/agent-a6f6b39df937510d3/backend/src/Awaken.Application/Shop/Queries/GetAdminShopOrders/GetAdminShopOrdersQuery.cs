using Awaken.Contracts.Common;
using Awaken.Contracts.Shop;
using MediatR;

namespace Awaken.Application.Shop.Queries.GetAdminShopOrders;

/// <summary>
/// US-193: consulta administrativa paginada de pedidos de compra.
/// Todos os filtros são opcionais; ordenação por CreatedAtUtc desc.
/// </summary>
public record GetAdminShopOrdersQuery(
    Guid? UserId = null,
    string? Status = null,
    string? Channel = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResponse<AdminShopOrderResponse>>;
