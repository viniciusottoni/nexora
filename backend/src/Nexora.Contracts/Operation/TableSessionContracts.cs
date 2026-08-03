using System.Text.Json.Serialization;
using Nexora.Contracts.Catalog;

namespace Nexora.Contracts.Operation;

/// <summary>
/// Porta de comanda (sessão de mesa) — usada por <c>POST /v1/tables/{id}/sessions</c>,
/// <c>PATCH /v1/sessions/{id}</c> e <c>GET /v1/sessions/{id}</c> (US-022), e reaproveitada por
/// <see cref="PublicTableAccessResponse"/> (US-021). <see cref="CurrentItems"/>/<see cref="Total"/>
/// sempre vazio/zero nesta wave — lançamento de item de pedido é escopo de US-030/US-028; o campo
/// já existe no contrato para o consumidor (web-menu) não precisar de uma quebra de contrato
/// quando esse escopo for implementado.
/// </summary>
/// <remarks>
/// [DESVIO DOCUMENTADO] US-022 §7 mostra a resposta de abertura envelopada
/// (<c>{ "session": {...} }</c>) e com um objeto <c>waiter</c> aninhado. Aqui a resposta é achatada
/// (sem envelope, <c>WaiterId</c> em vez de <c>waiter</c>) para seguir o mesmo padrão já usado por
/// <c>TableResponse</c>/<c>AreaResponse</c> (US-020) — nome/e-mail do garçom exigiriam um join com
/// <c>app_user</c> fora do escopo mínimo desta história; o cliente já tem o id para resolver o
/// nome via <c>/v1/users</c> caso precise.
/// </remarks>
public sealed record TableSessionResponse(
    Guid Id,
    Guid TableId,
    string TableLabel,
    string Status,
    DateTimeOffset OpenedAt,
    short GuestCount,
    bool GuestCountConfirmed,
    Guid? WaiterId,
    string Source,
    IReadOnlyList<TableSessionItemResponse> CurrentItems,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Total,
    // US-026 §8: preferência de divisão registrada na solicitação da conta — nulo até a primeira
    // solicitação (ou depois de TableSession.ReopenAfterNewItem, que zera os dois campos).
    string? SplitMode = null,
    short? SplitPeople = null);

/// <summary>Item já lançado na comanda (US-021 §7: <c>currentItems</c>) — lista vazia até US-030/US-028.</summary>
public sealed record TableSessionItemResponse(string Name, int Quantity, string Status);

/// <summary>Porta de <c>POST /v1/tables/{id}/sessions</c> — abertura pelo garçom (US-022 §7).</summary>
public sealed record OpenTableSessionRequest(short GuestCount);

/// <summary>
/// Porta de <c>PATCH /v1/sessions/{id}</c> (US-022 §7) — os dois campos são opcionais e
/// independentes: informar só <see cref="GuestCount"/> confirma/corrige a contagem (cenário
/// "Primeira leitura da mesa" da US-021, quando a origem foi QR); informar só
/// <see cref="WaiterId"/> troca o responsável (cenário "Troca de garçom responsável" da US-022).
/// </summary>
public sealed record UpdateTableSessionRequest(short? GuestCount, Guid? WaiterId);

/// <summary>
/// Porta de <c>GET /v1/public/table/{qrToken}</c> (US-021 §7) — resolve a mesa pelo token público,
/// abre sessão automaticamente quando ainda não há uma ativa (mesmo comando de US-022, com
/// <c>source=QR</c>) e devolve o <c>sessionToken</c> anônimo de escopo mínimo.
/// </summary>
public sealed record PublicTableAccessResponse(
    PublicTableInfoResponse Table,
    TableSessionResponse Session,
    string SessionToken);

public sealed record PublicTableInfoResponse(Guid Id, string Label, string AreaName);

/// <summary>Porta de <c>POST /v1/public/table/{qrToken}/call-waiter</c> e <c>POST /v1/tables/{id}/acknowledge-call</c> (US-025 §7).</summary>
public sealed record CallWaiterResponse(bool Acknowledged, bool AlreadyPending);

/// <summary>Porta de <c>POST /v1/tables/{id}/acknowledge-call</c> (US-025 §7) — <c>ResponseSeconds</c> é <c>RaisedAt</c> até o momento da confirmação.</summary>
public sealed record AcknowledgeWaiterCallResponse(bool Resolved, int ResponseSeconds);

/// <summary>
/// Porta de <c>POST /v1/public/table/{qrToken}/request-bill</c> e <c>POST /v1/sessions/{id}/request-bill</c>
/// (US-026 §7) — <see cref="SplitMode"/> é um de <c>"BY_PERSON"</c>/<c>"BY_ITEM"</c>/<c>"SINGLE"</c>
/// (validado por <c>RequestBillCommandValidator</c>/<c>RequestBillByQrCommandValidator</c>, não por
/// enum nativo — ver docstring de <c>TableSession.SplitMode</c>). <see cref="People"/> só é exigido
/// quando <see cref="SplitMode"/> é <c>"BY_PERSON"</c>.
/// </summary>
public sealed record RequestBillRequest(string SplitMode, short? People);

/// <summary>
/// <see cref="AlreadyRequested"/> é verdadeiro quando a sessão já estava em <c>BILL_REQUESTED</c>
/// (re-solicitação idempotente: só atualiza a preferência de divisão, sem gerar um segundo alerta
/// ao caixa — mesma lógica de "chamada repetida" da US-025, aplicada aqui).
/// </summary>
public sealed record RequestBillResponse(TableSessionResponse Session, bool AlreadyRequested);
