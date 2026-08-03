using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Commands.RequestBill;

/// <summary>
/// Porta de <c>POST /v1/sessions/{id}/request-bill</c> (US-026 §7, cenário "Solicitação pelo
/// garçom": "o efeito deve ser idêntico ao da solicitação pelo cliente" — ver
/// <c>RequestBillByQrCommand</c> para o gêmeo do cliente e <c>BillRequestCoordinator</c> para o
/// núcleo compartilhado). <see cref="AuthorizationToken"/>/<see cref="Reason"/> (US-035) só são
/// relevantes quando existe item pendente e o tenant está no modo <c>BLOCK</c> — o header
/// <c>X-Authorization-Token</c> (ADR-023) autoriza a ação <c>CLOSE_WITH_PENDING</c> e
/// <see cref="Reason"/> é o motivo gravado no <c>AuditLog</c> junto do autorizador.
/// </summary>
public sealed record RequestBillCommand(
    Guid SessionId,
    string SplitMode,
    short? People,
    string? AuthorizationToken = null,
    string? Reason = null) : ICommand<RequestBillResponse>;
