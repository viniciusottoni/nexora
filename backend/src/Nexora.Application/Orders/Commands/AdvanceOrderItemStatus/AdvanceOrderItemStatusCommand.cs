using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Orders.Commands.AdvanceOrderItemStatus;

/// <summary>
/// Porta de <c>POST /v1/orders/{orderId}/items/{itemId}/advance</c> — ver docstring de
/// <see cref="AdvanceOrderItemStatusCommandHandler"/>.
/// </summary>
/// <param name="OccurredAt">
/// US-032/ADR-034 — horário do header <c>X-Occurred-At</c> (relógio do dispositivo que disparou o
/// avanço), corrigido pelo handler contra o relógio do edge antes de ser gravado no carimbo. Nulo
/// quando o dispositivo não envia o header — cai no relógio do servidor.
/// </param>
public sealed record AdvanceOrderItemStatusCommand(Guid OrderId, Guid ItemId, DateTimeOffset? OccurredAt = null) : ICommand<OrderItemResponse>;
