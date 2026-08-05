using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Orders.Commands.UndoKdsItemAdvance;

/// <summary>
/// Porta de <c>POST /v1/kds/items/{itemId}/undo</c> (US-041 §3/§4, "Desfazer avanço acidental") —
/// janela de <see cref="Nexora.Application.Orders.Commands.UndoKdsItemAdvance.UndoKdsItemAdvanceCommandHandler.UndoWindow"/>
/// desde a última transição, checada contra <c>OrderItem.LastTransitionAt</c> (o próprio domínio
/// sabe qual carimbo corresponde ao estado atual — ver docstring do getter).
/// </summary>
public sealed record UndoKdsItemAdvanceCommand(Guid ItemId) : ICommand<OrderItemResponse>;
