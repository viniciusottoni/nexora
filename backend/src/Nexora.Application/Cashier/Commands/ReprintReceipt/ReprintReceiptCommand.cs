using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Cashier;

namespace Nexora.Application.Cashier.Commands.ReprintReceipt;

/// <summary>Porta de <c>POST /v1/sessions/{id}/receipt/reprint</c> (US-057 §4, cenário "Reimpressão auditada" — RN "reimpressão registrada em audit_log com autor e horário").</summary>
public sealed record ReprintReceiptCommand(Guid SessionId, string? PrinterId) : ICommand<PrintReceiptResponse>;
