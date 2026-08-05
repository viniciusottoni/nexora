using System.Text.Json.Serialization;
using Nexora.Contracts.Catalog;
using Nexora.Contracts.Operation;

namespace Nexora.Contracts.Cashier;

/// <summary>
/// Contratos de US-057 (Comprovante não fiscal de consumo) — <c>GET /v1/sessions/{id}/receipt</c>,
/// <c>POST /v1/sessions/{id}/receipt/print</c>, <c>POST /v1/sessions/{id}/receipt/reprint</c>.
/// <see cref="ReceiptResponse.IsFiscal"/> é SEMPRE <c>false</c> nesta wave — RN-023 é pendência
/// crítica (NFC-e/SAT), este comprovante nunca a substitui (US-057 §2/§15).
/// </summary>
public sealed record ReceiptPaymentResponse(
    string Method,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Amount);

public sealed record ReceiptResponse(
    string Url,
    string Number,
    bool IsFiscal,
    DateTimeOffset IssuedAt,
    IReadOnlyList<BillItemResponse> Items,
    IReadOnlyList<ReceiptPaymentResponse> Payments,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Subtotal,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal ServiceFee,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Discount,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Total);

public sealed record GetReceiptResponse(ReceiptResponse Receipt);

public sealed record PrintReceiptRequest(string? PrinterId = null);

public sealed record PrintReceiptResponse(bool Queued);
