using System.Text.Json.Serialization;
using Nexora.Contracts.Catalog;

namespace Nexora.Contracts.Cashier;

/// <summary>
/// Contratos de US-052 (Múltiplas formas de pagamento na mesma conta) e US-058 (Registrar
/// pagamento de maquininha externa) — <c>POST /v1/sessions/{id}/payments</c>. Dinheiro sempre
/// <c>decimal</c> serializado como string via <see cref="MoneyJsonConverter"/> (ADR-017), mesma
/// convenção de <c>Nexora.Contracts.Operation.BillContracts</c>.
/// </summary>
/// <param name="Method">Um de <c>CASH</c>/<c>CREDIT</c>/<c>DEBIT</c>/<c>PIX</c>/<c>VOUCHER</c>/<c>ONLINE</c>/<c>OTHER</c>.</param>
/// <param name="Amount">Valor efetivamente aplicado à conta — para <c>CASH</c> com troco, é o valor líquido (não o recebido, ver <see cref="ReceivedAmount"/>).</param>
/// <param name="ReceivedAmount">
/// US-052 §4, cenário "Troco em dinheiro" — só relevante para <c>CASH</c>: valor efetivamente
/// entregue pelo cliente, usado para calcular o troco (<c>ReceivedAmount - Amount</c>). Ausente/igual
/// a <see cref="Amount"/> quando não há troco.
/// </param>
/// <param name="Provider">US-058 — maquininha externa (ex.: <c>CIELO</c>, <c>MERCADO_PAGO</c>), ADR-024. Nulo para pagamento sem provedor (dinheiro).</param>
/// <param name="ProviderRef">US-058 — NSU/id da transação na maquininha. Opcional; ausência marca o pagamento como pendente de conciliação.</param>
/// <param name="Brand">US-058 — bandeira do cartão, opcional.</param>
/// <param name="Installments">US-058 — número de parcelas, padrão 1.</param>
/// <param name="ConfirmDuplicate">
/// US-058 §4, cenário "Referência duplicada": quando o mesmo <see cref="ProviderRef"/> já foi
/// registrado no turno, a primeira tentativa sem esta flag recebe um aviso (não bloqueio); reenviar
/// com <c>true</c> confirma o registro mesmo assim.
/// </param>
public sealed record PaymentRequest(
    string Method,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Amount,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal? ReceivedAmount = null,
    string? Provider = null,
    string? ProviderRef = null,
    string? Brand = null,
    int Installments = 1,
    bool ConfirmDuplicate = false);

public sealed record RegisterPaymentsRequest(IReadOnlyList<PaymentRequest> Payments);

/// <summary>Pagamento registrado, já com taxa/valor líquido calculados (US-058).</summary>
public sealed record RegisteredPaymentResponse(
    Guid Id,
    string Method,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Amount,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal NetAmount,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal FeeAmount,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal ChangeAmount,
    string? Provider,
    string? ProviderRef,
    string ReconciliationStatus);

public sealed record PaymentSessionStatusResponse(string Status);

public sealed record ReceiptReferenceResponse(string Url);

public sealed record RegisterPaymentsResponse(
    PaymentSessionStatusResponse Session,
    IReadOnlyList<RegisteredPaymentResponse> Payments,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Change,
    ReceiptReferenceResponse Receipt);
