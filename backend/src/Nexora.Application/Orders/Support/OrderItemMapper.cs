using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;

namespace Nexora.Application.Orders.Support;

/// <summary>Monta <see cref="OrderItemResponse"/> a partir do agregado — usado por <c>AddOrderItemCommandHandler</c> e <c>RepeatOrderItemCommandHandler</c>.</summary>
internal static class OrderItemMapper
{
    public static OrderItemResponse Map(OrderItem item, string productName) => new(
        item.Id,
        item.OrderId,
        item.VariantId,
        productName,
        item.Quantity,
        item.UnitPrice,
        item.TotalPrice,
        OrderItemStatusLabels.ToWireStatus(item.Status),
        item.Notes,
        item.StationId,
        item.RepeatedFromItemId,
        item.Modifiers.Select(m => new OrderItemModifierResponse(m.ModifierId, m.NameSnapshot, m.Quantity, m.PriceDelta)).ToList(),
        item.Fractions.Select(f => new OrderItemFractionResponse(f.VariantId, f.Weight, f.UnitPrice)).ToList());
}
