using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Orders.Queries.GetOrderItemTimeline;

/// <summary>Porta de <c>GET /v1/orders/{orderId}/items/{itemId}/timeline</c> — ver docstring de <see cref="GetOrderItemTimelineQueryHandler"/>.</summary>
public sealed record GetOrderItemTimelineQuery(Guid OrderId, Guid ItemId) : IQuery<OrderItemTimelineResponse>;
