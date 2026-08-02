using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Commands.RequestBill;

/// <summary>
/// Porta de <c>POST /v1/sessions/{id}/request-bill</c> (US-026 §7, cenário "Solicitação pelo
/// garçom": "o efeito deve ser idêntico ao da solicitação pelo cliente" — ver
/// <c>RequestBillByQrCommand</c> para o gêmeo do cliente e <c>BillRequestCoordinator</c> para o
/// núcleo compartilhado).
/// </summary>
public sealed record RequestBillCommand(Guid SessionId, string SplitMode, short? People) : ICommand<RequestBillResponse>;
