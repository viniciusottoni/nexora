using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Orders.Commands.AdvanceKdsItem;

/// <summary>
/// Porta de <c>POST /v1/kds/items/{itemId}/advance</c> (US-041 §7) — avança UM item específico
/// (toque direto no cartão, não o teclado numérico) sem exigir <c>orderId</c> na rota, diferente de
/// <see cref="Nexora.Application.Orders.Commands.AdvanceOrderItemStatus.AdvanceOrderItemStatusCommand"/>
/// (rota <c>/v1/orders/{orderId}/items/{itemId}/advance</c>, US-024 — mantida como está para não
/// quebrar quem já a consome). As duas reaproveitam a MESMA máquina de estado
/// (<see cref="Nexora.Application.Orders.Support.OrderItemStatusMachine"/>).
/// </summary>
public sealed record AdvanceKdsItemCommand(Guid ItemId, DateTimeOffset? OccurredAt = null) : ICommand<OrderItemResponse>;
