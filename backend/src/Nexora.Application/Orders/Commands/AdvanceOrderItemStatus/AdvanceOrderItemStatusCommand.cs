using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Orders.Commands.AdvanceOrderItemStatus;

/// <summary>Porta de <c>POST /v1/orders/{orderId}/items/{itemId}/advance</c> — ver docstring de <see cref="AdvanceOrderItemStatusCommandHandler"/>.</summary>
public sealed record AdvanceOrderItemStatusCommand(Guid OrderId, Guid ItemId) : ICommand<OrderItemResponse>;
