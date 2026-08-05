using Nexora.Domain.Operation;

namespace Nexora.Application.Orders.Support;

/// <summary>
/// Avança um <see cref="OrderItem"/> um passo, delegando ao método de domínio correspondente ao
/// estado atual (Queued→Fired→InOven→OutOfOven→Ready→Served). Extraído de
/// <c>AdvanceOrderItemStatusCommandHandler</c> (US-024) para ser reaproveitado, palavra por
/// palavra, pelos comandos de KDS da US-041 (<c>AdvanceKdsItemCommand</c>,
/// <c>AdvanceKdsOrderCommand</c>) — duas rotas diferentes (uma por <c>orderId+itemId</c>, outra por
/// código curto do pedido) não podem divergir em qual é o "próximo estado natural" de um item.
/// </summary>
internal static class OrderItemStatusMachine
{
    /// <summary>
    /// Chama o método de transição de domínio adequado ao <see cref="OrderItem.Status"/> atual.
    /// Devolve <see langword="false"/> quando o item já está em estado final (Served/Cancelled) —
    /// o chamador decide a mensagem/código de erro apropriado ao seu próprio contrato.
    /// </summary>
    public static bool TryAdvanceOneStep(OrderItem item, Guid actorId, DateTimeOffset occurredAt, Guid? deviceId)
    {
        switch (item.Status)
        {
            case OrderItemStatus.Queued:
                item.Fire(actorId, occurredAt, deviceId);
                return true;
            case OrderItemStatus.Fired:
                item.SendToOven(ovenSlot: null, ovenInBy: actorId, occurredAt: occurredAt, deviceId: deviceId);
                return true;
            case OrderItemStatus.InOven:
                item.TakeOutOfOven(ovenOutBy: actorId, occurredAt: occurredAt, deviceId: deviceId);
                return true;
            case OrderItemStatus.OutOfOven:
                item.MarkReady(actorId, occurredAt, deviceId);
                return true;
            case OrderItemStatus.Ready:
                item.MarkServed(actorId, occurredAt, deviceId);
                return true;
            default:
                return false;
        }
    }

    /// <summary>Itens que ainda podem avançar (não Served/Cancelled) — usado para "próximo elegível" no avanço por código do pedido (US-041) e para elegibilidade de all-day (US-043).</summary>
    public static bool IsActive(OrderItemStatus status) => status is not (OrderItemStatus.Served or OrderItemStatus.Cancelled);
}
