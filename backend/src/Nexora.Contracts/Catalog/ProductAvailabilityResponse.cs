namespace Nexora.Contracts.Catalog;

/// <summary>
/// Resposta de <c>POST /v1/catalog/products/:id/availability</c>,
/// <c>POST /v1/kds/products/:id/unavailable</c> e <c>POST /v1/kds/products/:id/available</c>
/// (US-015 §7) — estado de disponibilidade do produto após a operação.
/// </summary>
/// <param name="AffectedPendingItems">
/// US-044 §7/§4 (cenário "Pedidos já confirmados não mudam"): quantidade de itens de pedido ainda
/// em estados não finais (não <c>Served</c>/<c>Cancelled</c>) que já continham este produto no
/// momento da marcação — eles permanecem na fila, o operador é apenas informado de quantos existem
/// para decidir se trata pelo fluxo de cancelamento. Sempre <c>0</c> em
/// <c>MarkProductAvailableCommandHandler</c>/<c>ListUnavailableProductsQueryHandler</c> (não é
/// recalculado nesses dois caminhos — só <c>MarkProductUnavailableCommandHandler</c> preenche).
/// </param>
public sealed record ProductAvailabilityResponse(
    Guid ProductId,
    string ProductName,
    bool IsAvailable,
    string? UnavailableReason,
    DateTimeOffset? UnavailableSince,
    int AffectedPendingItems = 0);

/// <summary>
/// Resposta de <c>GET /v1/catalog/products/unavailable</c> (nuvem) e
/// <c>GET /v1/kds/products/unavailable</c> (edge) — "lista de itens indisponíveis sempre visível
/// ao garçom, no topo do cardápio" (US-015 §10) e ao gestor (painel de administração).
/// </summary>
public sealed record UnavailableProductsResponse(IReadOnlyList<ProductAvailabilityResponse> Items);
