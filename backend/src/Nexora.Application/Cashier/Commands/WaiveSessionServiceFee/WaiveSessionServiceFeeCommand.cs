using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Cashier;

namespace Nexora.Application.Cashier.Commands.WaiveSessionServiceFee;

/// <summary>
/// Porta de <c>POST /v1/sessions/{id}/service-fee/waive</c> (US-053) — registro AUTORITATIVO da
/// retirada da taxa de serviço no nível da sessão (RN-010), diferente da retirada efêmera de
/// US-027 (<see cref="Nexora.Application.Tables.Commands.WaiveServiceFee.WaiveServiceFeeCommand"/>,
/// que só recalcula uma prévia sem persistir). <see cref="Scope"/> <c>FULL</c> zera a taxa da conta
/// inteira; <c>PARTIAL</c> exige <see cref="Person"/> e uma divisão por pessoa já ativa na sessão.
/// </summary>
public sealed record WaiveSessionServiceFeeCommand(
    Guid SessionId,
    string Reason,
    string Scope,
    int? Person = null) : ICommand<WaiveSessionServiceFeeResponse>;
