using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Orders.Queries.GetOrder;

/// <summary>Porta de <c>GET /v1/orders/{id}</c> (US-030 §7).</summary>
public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderResponse>;
