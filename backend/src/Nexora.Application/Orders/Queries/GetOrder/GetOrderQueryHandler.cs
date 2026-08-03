using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Orders.Support;
using Nexora.Contracts.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Orders.Queries.GetOrder;

/// <summary>Porta de <c>GET /v1/orders/{id}</c> (US-030 §7) — devolve o pedido com os itens (ADR-021 princípio 7: "toda escrita devolve o estado resultante", este GET fecha o mesmo contrato para reconsulta).</summary>
internal sealed class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, Result<OrderResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetOrderQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<OrderResponse>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.Modifiers)
            .Include(o => o.Items).ThenInclude(i => i.Fractions)
            .Include(o => o.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Product)
            .SingleOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result<OrderResponse>.Failure("Pedido não encontrado.", ApiErrorCodes.OrderNotFound);
        }

        var items = order.Items
            .Select(item => OrderItemMapper.Map(item, $"{item.Variant.Product.Name} {item.Variant.Name}".Trim()))
            .ToList();

        var response = new OrderResponse(
            order.Id,
            order.ShortCode,
            OrderStatusLabels.ToWireStatus(order.Status),
            order.SessionId,
            order.Channel.ToString(),
            order.Total,
            order.PlacedAt,
            items);

        return Result<OrderResponse>.Success(response);
    }
}
