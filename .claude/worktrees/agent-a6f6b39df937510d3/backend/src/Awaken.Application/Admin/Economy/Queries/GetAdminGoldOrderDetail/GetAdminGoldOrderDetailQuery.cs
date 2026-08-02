using Awaken.Contracts.Admin.Economy;
using MediatR;

namespace Awaken.Application.Admin.Economy.Queries.GetAdminGoldOrderDetail;

/// <summary>
/// US-229: detalhe de um ShopOrder com lançamentos de ledger relacionados.
/// </summary>
public record GetAdminGoldOrderDetailQuery(Guid OrderId)
    : IRequest<GoldOrderDetailAdminResponse?>;
