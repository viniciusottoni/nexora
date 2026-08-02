using System.Text.Json.Serialization;
using Nexora.Contracts.Catalog;

namespace Nexora.Contracts.Operation;

/// <summary>
/// Contratos de US-027 (Dividir a conta) — <c>GET /v1/sessions/{id}/bill</c>,
/// <c>POST /v1/sessions/{id}/bill/assign-items</c>, <c>POST /v1/sessions/{id}/bill/waive-service-fee</c>
/// e <c>POST /v1/sessions/{id}/bill/partial-payment</c>. Money sempre <c>decimal</c> serializado como
/// string via <see cref="MoneyJsonConverter"/> (ADR-017) — a especificação original (US-027 §7) usa
/// centavos inteiros por ser um documento mais antigo do stack Node; o backend .NET atual usa
/// decimal/string em todo o resto da API, e este contrato segue a MESMA convenção para não
/// introduzir um segundo formato de dinheiro divergente.
/// </summary>
public sealed record BillItemResponse(
    Guid Id,
    string Name,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Total,
    bool Pending,
    int? AssignedPerson);

/// <summary>Item ainda em produção no momento da divisão (US-027 §4, cenário "Item pendente durante a divisão").</summary>
public sealed record BillPendingItemResponse(Guid Id, string Name, string Status);

/// <summary>Parte de uma pessoa na divisão — <see cref="ServiceFeeAmount"/> sempre identificado separadamente (US-027 §4).</summary>
public sealed record BillSplitPartResponse(
    int Person,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Amount,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal ServiceFeeAmount,
    bool ServiceFeeWaived);

/// <summary>
/// Porta de <c>GET /v1/sessions/{id}/bill</c> (staff) e <c>GET /v1/public/sessions/current/bill</c>
/// (cliente, pré-visualização) — mesmo formato para as duas audiências (US-027 §10: "Cliente pode
/// pré-visualizar a divisão no celular antes de o caixa começar").
/// </summary>
public sealed record BillResponse(
    IReadOnlyList<BillItemResponse> Items,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Subtotal,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal ServiceFee,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Total,
    string SplitMode,
    IReadOnlyList<BillSplitPartResponse> Split,
    IReadOnlyList<BillPendingItemResponse> PendingItems,
    bool HasPendingItems,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal? AmountPaid,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal? RemainingAmount,
    IReadOnlyList<Guid> UnassignedItemIds);

/// <summary>Atribuição de um conjunto de itens a uma pessoa — elemento de <see cref="AssignBillItemsRequest"/>.</summary>
public sealed record BillItemAssignmentRequest(int Person, IReadOnlyList<Guid> ItemIds);

/// <summary>
/// Porta de <c>POST /v1/sessions/{id}/bill/assign-items</c> (US-027 §7). [DECISÃO DE ARQUITETURA]
/// A atribuição NÃO é persistida — US-027 §6: "a divisão é cálculo, não fato de negócio; eventos
/// nascem no pagamento e na retirada de taxa". O cliente (POS) reenvia a atribuição completa a cada
/// chamada; o servidor só valida (nenhum item órfão) e devolve a <see cref="BillResponse"/> calculada.
/// <see cref="ServiceFeeWaivedPersons"/> é opcional — permite combinar atribuição por item com
/// retirada de taxa numa única chamada, sem exigir uma segunda ida ao servidor.
/// </summary>
public sealed record AssignBillItemsRequest(
    IReadOnlyList<BillItemAssignmentRequest> Assignments,
    IReadOnlyList<int>? ServiceFeeWaivedPersons = null);

/// <summary>
/// Porta de <c>POST /v1/sessions/{id}/bill/waive-service-fee</c> (US-027 §4, cenário "Retirada da
/// taxa por uma das partes") — escopo restrito ao modo <c>BY_PERSON</c> (único cenário Gherkin da
/// história para retirada). <see cref="AlreadyWaivedPersons"/> é o conjunto acumulado de pessoas já
/// isentas ANTES desta chamada (o cliente mantém esse estado, já que a divisão não é persistida) —
/// a resposta recalcula com <see cref="Person"/> somado a esse conjunto. <see cref="Reason"/> é
/// opcional mas recomendado (registrado no <c>AuditLog</c>, RN-010: "a retirada é registrada e auditada").
/// </summary>
public sealed record WaiveServiceFeeRequest(
    short People,
    int Person,
    IReadOnlyList<int>? AlreadyWaivedPersons,
    string? Reason);

/// <summary>Porta de <c>POST /v1/sessions/{id}/bill/partial-payment</c> (US-027 §4, cenário "Divisão por valor").</summary>
public sealed record RegisterPartialPaymentRequest(
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Amount,
    string Method);

/// <summary>
/// <see cref="RemainingAmount"/> é o saldo que continua em aberto — a sessão permanece em
/// <c>BILL_REQUESTED</c> (US-027 §4: "a sessão deve permanecer em BILL_REQUESTED"); fechar de fato
/// é US-052, fora desta história.
/// </summary>
public sealed record PartialPaymentResponse(
    Guid PaymentId,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal AmountPaid,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal RemainingAmount,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Total,
    string SessionStatus);
