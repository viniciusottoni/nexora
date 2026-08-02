namespace Nexora.Contracts.Catalog;

/// <summary>
/// Resposta de <c>POST /v1/catalog/products/:id/availability</c>,
/// <c>POST /v1/kds/products/:id/unavailable</c> e <c>POST /v1/kds/products/:id/available</c>
/// (US-015 §7) — estado de disponibilidade do produto após a operação.
/// </summary>
public sealed record ProductAvailabilityResponse(
    Guid ProductId,
    string ProductName,
    bool IsAvailable,
    string? UnavailableReason,
    DateTimeOffset? UnavailableSince);

/// <summary>
/// Resposta de <c>GET /v1/catalog/products/unavailable</c> (nuvem) e
/// <c>GET /v1/kds/products/unavailable</c> (edge) — "lista de itens indisponíveis sempre visível
/// ao garçom, no topo do cardápio" (US-015 §10) e ao gestor (painel de administração).
/// </summary>
public sealed record UnavailableProductsResponse(IReadOnlyList<ProductAvailabilityResponse> Items);
