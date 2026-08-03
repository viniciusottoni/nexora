using Nexora.Domain.Operation;

namespace Nexora.Application.Orders.Support;

/// <summary>Fio (wire format) de <see cref="OrderStatus"/> — mesma convenção de <see cref="OrderItemStatusLabels.ToWireStatus"/> (upper snake_case, ex.: <c>IN_PRODUCTION</c>).</summary>
public static class OrderStatusLabels
{
    public static string ToWireStatus(OrderStatus status) => status switch
    {
        OrderStatus.Draft => "DRAFT",
        OrderStatus.Placed => "PLACED",
        OrderStatus.InProduction => "IN_PRODUCTION",
        OrderStatus.Ready => "READY",
        OrderStatus.Dispatched => "DISPATCHED",
        OrderStatus.Delivered => "DELIVERED",
        OrderStatus.Closed => "CLOSED",
        OrderStatus.Cancelled => "CANCELLED",
        _ => status.ToString().ToUpperInvariant(),
    };
}
