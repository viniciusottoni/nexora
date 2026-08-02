using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Commands.AssignBillItems;

/// <summary>Atribuição de um conjunto de itens a uma pessoa — ver docstring de <see cref="AssignBillItemsCommand"/>.</summary>
public sealed record BillItemAssignmentInput(int Person, IReadOnlyList<Guid> ItemIds);

/// <summary>
/// Porta de <c>POST /v1/sessions/{id}/bill/assign-items</c> (US-027 §7, modo <c>BY_ITEM</c>).
/// [DECISÃO DE ARQUITETURA] Não persiste a atribuição (US-027 §6: "a divisão é cálculo, não fato de
/// negócio") — só valida (nenhum item cancelado/de outra sessão fica órfão, nenhum item atribuído a
/// duas pessoas) e devolve a <see cref="BillResponse"/> calculada. Recusa com
/// <c>BILL_ITEM_NOT_ASSIGNED</c> quando sobra item sem dono (RN-017).
/// </summary>
public sealed record AssignBillItemsCommand(
    Guid SessionId,
    IReadOnlyList<BillItemAssignmentInput> Assignments,
    IReadOnlyList<int>? ServiceFeeWaivedPersons) : ICommand<BillResponse>;
