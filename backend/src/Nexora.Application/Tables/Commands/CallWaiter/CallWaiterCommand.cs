using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Commands.CallWaiter;

/// <summary>
/// Porta de <c>POST /v1/public/table/{qrToken}/call-waiter</c> (US-025 §7) — <see cref="SessionId"/>/
/// <see cref="TableId"/> vêm das claims <c>ses</c>/<c>tbl</c> do PRÓPRIO token de sessão anônimo
/// (esquema <c>TableSession</c>, policy <c>SessionScope</c>), nunca do corpo da requisição (RN-015:
/// "nenhum desses endpoints recebe sessionId no corpo, só resolve pelo token"). <see cref="QrToken"/>
/// é o segmento da rota — o handler confirma que ele ainda resolve para a MESMA mesa do token antes
/// de agir, e devolve sempre 404 (nunca 403) se algo não bater (mesmo padrão de
/// <c>AccessTableByQrTokenCommand</c>).
/// </summary>
public sealed record CallWaiterCommand(string QrToken, Guid SessionId, Guid TableId) : ICommand<CallWaiterResponse>;
