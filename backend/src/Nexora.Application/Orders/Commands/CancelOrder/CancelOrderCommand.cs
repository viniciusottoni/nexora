using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Orders.Commands.CancelOrder;

/// <summary>
/// Porta de <c>POST /v1/orders/{id}/cancel</c> (US-033 §7) — cancela o PEDIDO INTEIRO, todos os
/// itens não cancelados/servidos na MESMA operação (§4, cenário "Cancelamento de pedido inteiro").
/// <paramref name="AuthorizationToken"/> segue a mesma convenção OPCIONAL de
/// <see cref="Nexora.Application.Orders.Commands.CancelOrderItem.CancelOrderItemCommand"/> — só é
/// exigido quando QUALQUER item ativo do pedido já foi iniciado.
/// </summary>
/// <remarks>
/// Implementa <see cref="IPersistsStateOnFailureCommand"/> pelo mesmo motivo de
/// <c>CancelOrderItemCommand</c> — a tentativa negada de autorização precisa sobreviver ao
/// rollback padrão de <c>TransactionBehavior</c>.
/// </remarks>
public sealed record CancelOrderCommand(
    Guid OrderId,
    string Reason,
    string? Notes,
    string? AuthorizationToken) : ICommand<CancelOrderResponse>, IPersistsStateOnFailureCommand;
