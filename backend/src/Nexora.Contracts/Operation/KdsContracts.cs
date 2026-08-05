namespace Nexora.Contracts.Operation;

/// <summary>
/// Contratos da US-031 (Roteamento simultâneo para cozinha e caixa) — arquivo dedicado (em vez de
/// <c>OrderContracts.cs</c>) para não colidir com outras histórias em paralelo desta mesma onda que
/// também tocam pedido/item (US-033 cancelamento, US-035 bloqueio de fechamento).
/// </summary>
public sealed record KdsQueueItemResponse(
    Guid OrderItemId,
    Guid OrderId,
    string OrderCode,
    /// <summary>US-044 §3 ("marcar indisponível a partir do cartão") — id do Produto (não da variação; ver desvio documentado em <c>ProductAvailabilityController</c>), necessário para acionar <c>POST /v1/kds/products/{'{'}productId{'}'}/unavailable</c> direto do item da fila.</summary>
    Guid ProductId,
    string ProductName,
    short Quantity,
    IReadOnlyList<string> Modifiers,
    string? Notes,
    string Status,
    DateTimeOffset PlacedAt,
    int ElapsedSeconds,
    /// <summary>
    /// US-040 §5 (escalonamento de cor) — "NORMAL"/"WARNING"/"CRITICAL", já resolvido no servidor
    /// a partir de <c>ProductVariant.WarnMinutes</c>/<c>CriticalMinutes</c> com herança do padrão
    /// do tenant (mesma resolução de <c>GetVariantPrepTimeAnalysisQueryHandler</c>, US-016) — o
    /// cliente nunca recalcula limiar, só lê o rótulo e troca de cor.
    /// </summary>
    string ThresholdState,
    /// <summary>
    /// US-040 §5 — limiares efetivos EM SEGUNDOS (mesma resolução de <see cref="ThresholdState"/>),
    /// para o cliente animar o cronômetro continuamente entre polls sem recalcular limiar algum —
    /// só compara contra o `elapsedSeconds` que ele já incrementa localmente (doc. US-040 §7:
    /// "o cliente apenas incrementa localmente").
    /// </summary>
    int WarnSeconds,
    int CriticalSeconds,
    string? Table,
    string Channel,
    /// <summary>US-040 §4 ("meio a meio no cartão") — frações do item, vazio para item sem fração.</summary>
    IReadOnlyList<KdsQueueItemFractionResponse> Fractions);

public sealed record KdsQueueItemFractionResponse(string ProductName, decimal Weight);

/// <summary>
/// Porta de <c>GET /v1/kds/queue?stationId=...&amp;since=...</c> (US-031 §7, fallback de polling do
/// ADR-011) — <see cref="LastEventId"/> é o cursor opaco que o cliente deve devolver na próxima
/// chamada (<c>since</c>) ou em <c>KdsHub.Resume</c>; hoje é sempre um snapshot completo da fila
/// ATIVA da praça (ver docstring de <c>GetKdsQueueQueryHandler</c>/<c>KdsHub.Resume</c> para a
/// decisão de escopo "snapshot, não delta").
/// </summary>
public sealed record GetKdsQueueResponse(IReadOnlyList<KdsQueueItemResponse> Items, string LastEventId);

/// <summary>Porta de <c>POST /v1/kds/orders/{shortCode}/advance</c> (US-041) — um item avançado (padrão) ou vários (confirmação de lote), ver docstring de <c>AdvanceKdsOrderCommand</c>.</summary>
public sealed record AdvanceKdsOrderResponse(IReadOnlyList<OrderItemResponse> Advanced);

/// <summary>
/// Corpo de <c>POST /v1/kds/orders/{shortCode}/advance</c> — <see cref="StationId"/> é obrigatório
/// porque o servidor não infere sozinho a praça do terminal (mesma convenção de
/// <c>GET /v1/kds/queue?stationId=...</c>: o cliente já sabe a própria praça pela claim do token,
/// e o servidor só a usa para filtrar). <see cref="Batch"/> (padrão <see langword="false"/>) é a
/// confirmação explícita de "avanço em lote" da US-041 §3.
/// </summary>
public sealed record AdvanceKdsOrderRequest(Guid StationId, bool Batch = false);
