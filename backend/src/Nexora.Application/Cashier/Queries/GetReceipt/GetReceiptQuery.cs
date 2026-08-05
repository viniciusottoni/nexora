using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Cashier;

namespace Nexora.Application.Cashier.Queries.GetReceipt;

/// <summary>
/// Porta de <c>GET /v1/sessions/{id}/receipt</c> (US-057) — comprovante NÃO FISCAL de consumo
/// (RN-023, pendência crítica de emissão fiscal fora de escopo desta wave). Só existe depois que a
/// conta foi paga (US-052) — antes disso não há pagamentos para discriminar.
/// </summary>
public sealed record GetReceiptQuery(Guid SessionId) : IQuery<GetReceiptResponse>;
