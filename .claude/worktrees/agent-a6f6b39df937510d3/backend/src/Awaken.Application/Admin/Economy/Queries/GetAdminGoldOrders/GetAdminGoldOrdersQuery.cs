using Awaken.Contracts.Admin.Economy;
using MediatR;

namespace Awaken.Application.Admin.Economy.Queries.GetAdminGoldOrders;

/// <summary>
/// US-229: listagem paginada admin de ShopOrder no canal gold.
/// </summary>
public record GetAdminGoldOrdersQuery(
    Guid? UserId,
    string? Status,
    string? ProductKey,
    DateTime? DateFrom,
    DateTime? DateTo,
    int Page,
    int PageSize)
    : IRequest<GoldOrderPageResponse>;
