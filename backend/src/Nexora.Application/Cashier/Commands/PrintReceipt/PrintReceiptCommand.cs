using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Cashier;

namespace Nexora.Application.Cashier.Commands.PrintReceipt;

/// <summary>
/// Porta de <c>POST /v1/sessions/{id}/receipt/print</c> (US-057 §4, cenário "Impressora
/// indisponível"): o pagamento (US-052) NUNCA depende deste comando — falha de impressora aqui
/// nunca bloqueia o recebimento, por design (a chamada é sempre posterior e independente). Sem
/// hardware real de impressora térmica definido ainda (ADR-026 só estabelece a abstração), este
/// comando só enfileira a intenção — 202 sempre, nunca falha por causa do dispositivo físico.
/// </summary>
public sealed record PrintReceiptCommand(Guid SessionId, string? PrinterId) : ICommand<PrintReceiptResponse>;
