using System.Text.Json.Serialization;
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
