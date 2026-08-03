using System.Text.Json.Serialization;
using Nexora.Contracts.Auth;
using Nexora.Contracts.Catalog;

namespace Nexora.Contracts.Operation;

/// <summary>
/// Contratos de US-024 (Consumo da mesa em tempo real) e US-028 (Repetir item com um toque).
/// <see cref="AddOrderItemRequest"/>/<see cref="OrderItemResponse"/> sustentam a capacidade MÍNIMA
/// de "lançar item no pedido de uma sessão" (gap de US-030 — ver
/// <c>Nexora.Application.Orders.Commands.AddOrderItem.AddOrderItemCommandHandler</c>), reaproveitada
/// pelo comando de repetição.
/// </summary>
public sealed record AddOrderItemModifierRequest(Guid ModifierId, short Quantity);

public sealed record AddOrderItemFractionRequest(Guid VariantId, decimal Weight);

/// <summary>Porta de <c>POST /v1/sessions/{sessionId}/items</c>.</summary>
public sealed record AddOrderItemRequest(
    Guid VariantId,
    short Quantity,
    string? Notes,
    IReadOnlyList<AddOrderItemModifierRequest>? Modifiers,
    IReadOnlyList<AddOrderItemFractionRequest>? Fractions);

public sealed record OrderItemModifierResponse(Guid ModifierId, string Name, short Quantity,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal PriceDelta);

public sealed record OrderItemFractionResponse(Guid VariantId, decimal Weight,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal UnitPrice);

/// <summary>Item lançado — retorno de <c>POST /v1/sessions/{sessionId}/items</c> e de <c>POST .../repeat</c> (US-028 §7: <c>item.unitPrice</c>/<c>item.repeatedFrom</c>).</summary>
public sealed record OrderItemResponse(
    Guid Id,
    Guid OrderId,
    Guid VariantId,
    string Name,
    short Quantity,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal UnitPrice,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal TotalPrice,
    string Status,
    string? Notes,
    Guid? StationId,
    Guid? RepeatedFromItemId,
    IReadOnlyList<OrderItemModifierResponse> Modifiers,
    IReadOnlyList<OrderItemFractionResponse> Fractions);

/// <summary>Envelope exato do contrato da US-028 §7: <c>{ "item": {...} }</c>.</summary>
public sealed record RepeatOrderItemResponse(OrderItemResponse Item);

/// <summary>Item da lista de consumo (US-024 §7) — status já traduzido para a linguagem do cliente.</summary>
public sealed record SessionConsumptionItemResponse(
    Guid OrderItemId,
    Guid OrderId,
    string Name,
    int Quantity,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal UnitPrice,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Total,
    string Status,
    string StatusLabel,
    int? EtaMinutes,
    bool Cancelled,
    Guid VariantId,
    bool ProductAvailable);

/// <summary>Porta de <c>GET /v1/public/sessions/current</c> (US-024 §7).</summary>
public sealed record SessionConsumptionResponse(
    IReadOnlyList<SessionConsumptionItemResponse> Items,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Subtotal,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal ServiceFee,
    bool ServiceFeeOptional,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Total,
    DateTimeOffset OpenedAt,
    int MinutesOpen);

/// <summary>
/// US-032 (Carimbos de tempo T0 a T5) §7 — autor de um carimbo da timeline. Ausente (nulo) quando
/// a transição não tem autor identificado (ex.: ambiente de teste, job interno).
/// </summary>
public sealed record OrderItemTimelineActorResponse(Guid Id, string Name);

/// <summary>Dispositivo de origem de um carimbo da timeline (RN-004) — ausente (nulo) pelo mesmo motivo de <see cref="OrderItemTimelineActorResponse"/>.</summary>
public sealed record OrderItemTimelineDeviceResponse(Guid Id, string Label);

/// <summary>Um dos seis carimbos T0 a T5 — <c>At</c> nulo quando o item ainda não passou por esta transição (ex.: <c>ovenInAt</c>/<c>ovenOutAt</c> de um item que não passa pelo gargalo).</summary>
public sealed record OrderItemTimestampResponse(
    DateTimeOffset? At,
    OrderItemTimelineActorResponse? Actor,
    OrderItemTimelineDeviceResponse? Device);

public sealed record OrderItemTimelineTimestampsResponse(
    OrderItemTimestampResponse PlacedAt,
    OrderItemTimestampResponse FiredAt,
    OrderItemTimestampResponse OvenInAt,
    OrderItemTimestampResponse OvenOutAt,
    OrderItemTimestampResponse ReadyAt,
    OrderItemTimestampResponse ServedAt);

/// <summary>MET-001 a MET-007 (US-032 §8) — cada intervalo é <c>null</c>, nunca zero/negativo, quando um dos dois carimbos que o compõem ainda não aconteceu.</summary>
public sealed record OrderItemTimelineDurationsResponse(
    int? QueueSeconds,
    int? AssemblySeconds,
    int? CookSeconds,
    int? FinishSeconds,
    int? ServeSeconds,
    int? PrepSeconds,
    int? TotalSeconds);

/// <summary>Porta de <c>GET /v1/orders/{id}/items/{itemId}/timeline</c> (US-032 §7) — drill-down do painel (RF-BI-11).</summary>
public sealed record OrderItemTimelineResponse(
    Guid OrderItemId,
    OrderItemTimelineTimestampsResponse Timestamps,
    OrderItemTimelineDurationsResponse Durations);

/// <summary>
/// US-030 (Criar pedido com itens, modificadores e frações) §7 — item de <c>POST /v1/orders</c> e
/// <c>POST /v1/public/orders</c>. Mesma forma de <see cref="AddOrderItemRequest"/> (sem
/// <c>VariantId</c>/<c>Quantity</c>/<c>Notes</c>/<c>Modifiers</c>/<c>Fractions</c> reaproveitados
/// de tipo — dois registros porque nascem de contratos distintos e podem evoluir separadamente).
/// </summary>
public sealed record CreateOrderItemRequest(
    Guid VariantId,
    short Quantity,
    string? Notes,
    IReadOnlyList<AddOrderItemModifierRequest>? Modifiers,
    IReadOnlyList<AddOrderItemFractionRequest>? Fractions);

/// <summary>
/// Porta de <c>POST /v1/orders</c> (US-030 §7) — canal e comanda (mesa) explícitos no corpo:
/// <c>sessionId</c> nulo para canais sem mesa (balcão/delivery/marketplace, Fase 1 sem fluxo
/// completo desses canais mas o contrato já aceita). <c>channel</c> segue a mesma convenção de fio
/// já usada pelo resto da solution (<c>"DineIn"</c>/<c>"Delivery"</c>/<c>"Takeout"</c>/
/// <c>"Marketplace"</c> — nome do enum C#, ver <c>VariantJsonContractTests</c>/<c>PricingJsonContractTests</c>
/// — NÃO o <c>"DINE_IN"</c> ilustrado na spec narrativa da história, que diverge da convenção real
/// já em produção no resto do catálogo).
/// </summary>
public sealed record CreateOrderRequest(
    string Channel,
    Guid? SessionId,
    IReadOnlyList<CreateOrderItemRequest> Items);

/// <summary>
/// Porta de <c>POST /v1/public/orders</c> (US-030 §7, caminho do cliente na mesa) — SEM
/// <c>channel</c>/<c>sessionId</c>: os dois vêm das claims do token de sessão de mesa (RN-015,
/// mesmo padrão de <c>CallWaiterCommand</c>/<c>RequestBillByQrCommand</c>), nunca do corpo que o
/// cliente controla.
/// </summary>
public sealed record CreatePublicOrderRequest(IReadOnlyList<CreateOrderItemRequest> Items);

/// <summary>Pedido dentro do envelope de <c>POST /v1/orders</c> (US-030 §7) — só um campo de código (<c>shortCode</c>, ex.: "A47"); o modelo de dados real (<c>Docs/Domain/03-Operacao.md</c>) não tem uma coluna "code" separada.</summary>
public sealed record OrderResponse(
    Guid Id,
    string ShortCode,
    string Status,
    Guid? SessionId,
    string Channel,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal Total,
    DateTimeOffset? PlacedAt,
    IReadOnlyList<OrderItemResponse> Items);

/// <summary>Envelope exato do contrato da US-030 §7: <c>{ "order": {...}, "promisedAt": ..., "estimatedMinutes": ... }</c>.</summary>
public sealed record CreateOrderResponse(
    OrderResponse Order,
    DateTimeOffset PromisedAt,
    int EstimatedMinutes);

/// <summary>
/// US-033 (Cancelar item ou pedido com autorização) §7/§10 — porta de <c>PATCH
/// /v1/orders/{id}/items/{itemId}/cancel</c>. <c>Reason</c> é o código de uma lista curta e
/// configurável (US-033 §10, ex.: <c>"CUSTOMER_REQUEST"</c>) — a lista em si mora no cliente
/// (Fase 1), nunca hardcoded no domínio (ADR-013). <c>Notes</c> é a observação livre opcional.
/// </summary>
public sealed record CancelOrderItemRequest(string Reason, string? Notes);

/// <summary>
/// Item cancelado — porta do envelope exato da US-033 §7: <c>{ "item": { "status": "CANCELLED",
/// "cancelledAt": ..., "wasStarted": ..., "authorizedBy": {...} } }</c>. <c>WasStarted</c> é
/// derivado do estado do item IMEDIATAMENTE ANTES do cancelamento (RN-008: item já iniciado não
/// estorna insumo — gera registro de perda, US-105/Fase 2, fora de escopo aqui) — não é uma coluna
/// persistida em <c>OrderItem</c> (ver docstring de <c>CancelOrderItemCommandHandler</c>).
/// </summary>
public sealed record CancelledOrderItemResponse(
    Guid Id,
    string Status,
    DateTimeOffset CancelledAt,
    string Reason,
    string? Notes,
    bool WasStarted,
    AuthorizedBySummary? AuthorizedBy);

/// <summary>Envelope exato do contrato da US-033 §7: <c>{ "item": {...} }</c>.</summary>
public sealed record CancelOrderItemResponse(CancelledOrderItemResponse Item);

/// <summary>US-033 §7 — porta de <c>POST /v1/orders/{id}/cancel</c> (cancelamento do pedido inteiro).</summary>
public sealed record CancelOrderRequest(string Reason, string? Notes);

/// <summary>
/// Pedido cancelado, com o detalhe de cada item cancelado na mesma operação (US-033 §4, cenário
/// "Cancelamento de pedido inteiro": "todos os itens devem ser cancelados na mesma operação").
/// </summary>
public sealed record CancelledOrderResponse(
    Guid Id,
    string Status,
    DateTimeOffset CancelledAt,
    string Reason,
    AuthorizedBySummary? AuthorizedBy,
    IReadOnlyList<CancelledOrderItemResponse> Items);

/// <summary>Envelope: <c>{ "order": {...} }</c> — mesma convenção de <see cref="CancelOrderItemResponse"/>.</summary>
public sealed record CancelOrderResponse(CancelledOrderResponse Order);
