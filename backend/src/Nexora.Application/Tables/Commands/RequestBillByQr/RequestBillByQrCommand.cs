using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Commands.RequestBillByQr;

/// <summary>
/// Porta de <c>POST /v1/public/table/{qrToken}/request-bill</c> (US-026 §7, cenário "Solicitação
/// pelo cliente") — <see cref="SessionId"/>/<see cref="TableId"/> vêm das claims <c>ses</c>/<c>tbl</c>
/// do token de sessão anônimo (RN-015, mesmo padrão de <c>CallWaiterCommand</c>), nunca do corpo.
/// </summary>
public sealed record RequestBillByQrCommand(string QrToken, Guid SessionId, Guid TableId, string SplitMode, short? People)
    : ICommand<RequestBillResponse>;
