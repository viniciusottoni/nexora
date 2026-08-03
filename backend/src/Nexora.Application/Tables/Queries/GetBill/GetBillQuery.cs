using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Queries.GetBill;

/// <summary>
/// Porta de <c>GET /v1/sessions/{id}/bill</c> (US-027 §7, staff — caixa/garçom). Os quatro
/// parâmetros de query SOBREPÕEM a preferência registrada em <c>TableSession.SplitMode</c>/
/// <c>SplitPeople</c> (US-026) quando informados — a US-027 §10 permite trocar de modo na hora de
/// ver a prévia, sem precisar de uma nova solicitação de conta. <see cref="Waived"/> é uma lista de
/// números de pessoa separada por vírgula (ex. <c>"1,3"</c>) que optaram por não pagar a taxa de
/// serviço — só relevante em <c>BY_PERSON</c>/<c>BY_ITEM</c>.
/// </summary>
public sealed record GetBillQuery(
    Guid SessionId,
    string? SplitMode,
    short? People,
    decimal? Amount,
    string? Waived) : IQuery<BillResponse>;
