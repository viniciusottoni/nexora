using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Orders.Commands.CancelOrderItem;

/// <summary>
/// Porta de <c>PATCH /v1/orders/{orderId}/items/{itemId}/cancel</c> (US-033 §7).
/// <paramref name="AuthorizationToken"/> é o valor CRU do header opcional <c>X-Authorization-Token</c>
/// (ADR-023) — opcional na borda HTTP porque a exigência é CONDICIONAL ao estado do item
/// (<see cref="Nexora.Application.Orders.Support.OrderCancellationPolicy"/>), algo que só o
/// handler sabe depois de carregar o item; por isso este comando NÃO usa o filtro estático
/// <c>[RequiresAuthorizationToken]</c> (esse exige o header sempre) — o handler chama o MESMO
/// <see cref="Nexora.Application.Abstractions.Security.IAuthorizationTokenValidator"/> diretamente,
/// só quando precisa.
/// </summary>
/// <remarks>
/// Implementa <see cref="IPersistsStateOnFailureCommand"/> (mesmo mecanismo de
/// <c>PairDeviceCommand</c>) — quando a autorização é negada, o handler registra uma tentativa em
/// <c>audit_log</c> (US-033 §4, cenário "Autorização negada": "a tentativa deve ser registrada em
/// audit_log") ANTES de devolver a falha; sem este marcador, <c>TransactionBehavior</c> reverteria
/// a transação e a tentativa negada nunca chegaria ao banco.
/// </remarks>
public sealed record CancelOrderItemCommand(
    Guid OrderId,
    Guid ItemId,
    string Reason,
    string? Notes,
    string? AuthorizationToken) : ICommand<CancelOrderItemResponse>, IPersistsStateOnFailureCommand;
