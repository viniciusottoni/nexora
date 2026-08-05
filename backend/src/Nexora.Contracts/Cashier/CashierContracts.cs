using System.Text.Json.Serialization;
using Nexora.Contracts.Catalog;

namespace Nexora.Contracts.Cashier;

/// <summary>
/// Porta de sessão de caixa (US-055 §7) — <see cref="Status"/> é <c>OPEN</c>/<c>CLOSING</c>/<c>CLOSED</c>
/// (mesmo vocabulário de <c>Nexora.Domain.Cashier.CashSessionStatus</c>, em maiúsculas para o contrato
/// de fio, mesma convenção de <c>TableSessionResponse.Status</c>).
/// </summary>
public sealed record CashSessionResponse(
    Guid Id,
    Guid OperatorId,
    string Status,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal OpeningAmount,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    [property: JsonConverter(typeof(NullableMoneyJsonConverter))] decimal? ExpectedAmount,
    [property: JsonConverter(typeof(NullableMoneyJsonConverter))] decimal? CountedAmount,
    [property: JsonConverter(typeof(NullableMoneyJsonConverter))] decimal? Divergence,
    string? Justification);

/// <summary>Porta de <c>POST /v1/cash-sessions/open</c> (US-055 §7).</summary>
public sealed record OpenCashSessionRequest(
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal OpeningAmount);

public sealed record OpenCashSessionResponse(CashSessionResponse Session);

/// <summary>
/// Composição do valor esperado em caixa (US-055 §7/§10: "a composição deve estar detalhada na
/// tela") — apuração do documento 04 §"Apuração do fechamento": abertura + pagamentos em dinheiro
/// pagos (já líquidos de troco) + suprimentos − sangrias. <see cref="Withdrawals"/> já carrega o
/// sinal negativo (mesmo formato do exemplo do contrato da US-055 §7: <c>"withdrawals": -15000</c>),
/// então <see cref="Total"/> = <see cref="Opening"/> + <see cref="CashPayments"/> + <see cref="Supplies"/> + <see cref="Withdrawals"/>.
/// </summary>
public sealed record CashExpectedAmountResponse(
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Opening,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal CashPayments,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Supplies,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Withdrawals,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Total);

/// <summary>Porta de <c>GET /v1/cash-sessions/current</c> (US-055 §7) — sessão aberta/em conferência do operador corrente na loja.</summary>
public sealed record GetCurrentCashSessionResponse(CashSessionResponse Session, CashExpectedAmountResponse Expected);

/// <summary>
/// Porta de <c>POST /v1/cash-sessions/{id}/close</c> (US-055 §7). <see cref="Justification"/> é
/// obrigatória quando a divergência ultrapassa o limiar configurado (<c>CashPolicy</c>) — validado
/// no handler, não no FluentValidation (depende do valor esperado, calculado em tempo de execução).
/// </summary>
public sealed record CloseCashSessionRequest(
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal CountedAmount,
    string? Justification = null);

public sealed record CloseCashSessionResponse(
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Expected,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Counted,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Divergence,
    bool RequiresJustification,
    CashSessionResponse Session);

/// <summary>Mesa ainda aberta que bloqueia o fechamento do caixa (RN-018) — <c>meta.openSessions</c> do 422 <see cref="Errors.ApiErrorCodes.OpenTables"/>-equivalente (US-055 §7).</summary>
public sealed record OpenTableSessionInfo(
    string Table,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Total);

/// <summary>Movimento de caixa (sangria/suprimento) — porta de US-056 §7.</summary>
public sealed record CashMovementResponse(
    Guid Id,
    string Type,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Amount,
    string Reason,
    DateTimeOffset OccurredAt,
    Guid CreatedBy,
    Guid? AuthorizedBy);

/// <summary>Porta de <c>POST /v1/cash-sessions/movements</c> (US-056 §7) — <see cref="Type"/> é <c>WITHDRAWAL</c> (sangria) ou <c>SUPPLY</c> (suprimento).</summary>
public sealed record RegisterCashMovementRequest(
    string Type,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Amount,
    string Reason);

public sealed record RegisterCashMovementResponse(
    CashMovementResponse Movement,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal NewExpected);

/// <summary>Porta de <c>GET /v1/cash-sessions/current/movements</c> (US-056 §7/§10: "histórico do turno acessível na mesma tela").</summary>
public sealed record ListCashMovementsResponse(IReadOnlyList<CashMovementResponse> Movements);
