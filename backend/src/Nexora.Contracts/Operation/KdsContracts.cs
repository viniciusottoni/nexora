namespace Nexora.Contracts.Operation;

/// <summary>
/// Contratos da US-031 (Roteamento simultâneo para cozinha e caixa) — arquivo dedicado (em vez de
/// <c>OrderContracts.cs</c>) para não colidir com outras histórias em paralelo desta mesma onda que
/// também tocam pedido/item (US-033 cancelamento, US-035 bloqueio de fechamento).
/// </summary>
public sealed record KdsQueueItemResponse(
    Guid OrderItemId,
    string OrderCode,
    string ProductName,
    short Quantity,
    IReadOnlyList<string> Modifiers,
    string? Notes,
    string Status,
    DateTimeOffset PlacedAt,
    int ElapsedSeconds,
    string? Table,
    string Channel);

/// <summary>
/// Porta de <c>GET /v1/kds/queue?stationId=...&amp;since=...</c> (US-031 §7, fallback de polling do
/// ADR-011) — <see cref="LastEventId"/> é o cursor opaco que o cliente deve devolver na próxima
/// chamada (<c>since</c>) ou em <c>KdsHub.Resume</c>; hoje é sempre um snapshot completo da fila
/// ATIVA da praça (ver docstring de <c>GetKdsQueueQueryHandler</c>/<c>KdsHub.Resume</c> para a
/// decisão de escopo "snapshot, não delta").
/// </summary>
public sealed record GetKdsQueueResponse(IReadOnlyList<KdsQueueItemResponse> Items, string LastEventId);
