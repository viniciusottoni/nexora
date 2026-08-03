using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Commands.WaiveServiceFee;

/// <summary>
/// Porta de <c>POST /v1/sessions/{id}/bill/waive-service-fee</c> (US-027 §4, cenário "Retirada da
/// taxa por uma das partes") — escopo restrito ao modo <c>BY_PERSON</c> (único cenário Gherkin da
/// história para retirada de taxa). <see cref="AlreadyWaivedPersons"/> é o conjunto de pessoas já
/// isentas antes desta chamada; a resposta soma <see cref="Person"/> a esse conjunto e recalcula.
/// RN-010: a retirada é sempre registrada e auditada (<c>AuditLog</c>, autor = usuário autenticado).
/// </summary>
public sealed record WaiveServiceFeeCommand(
    Guid SessionId,
    short People,
    int Person,
    IReadOnlyList<int>? AlreadyWaivedPersons,
    string? Reason) : ICommand<BillResponse>;
