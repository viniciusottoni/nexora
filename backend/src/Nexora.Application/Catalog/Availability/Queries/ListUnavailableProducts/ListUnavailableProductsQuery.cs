using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Availability.Queries.ListUnavailableProducts;

/// <summary>
/// Porta de <c>GET /v1/catalog/products/unavailable</c> (nuvem) e
/// <c>GET /v1/kds/products/unavailable</c> (edge) — "lista de itens indisponíveis sempre visível
/// ao garçom, no topo do cardápio" (US-015 §10) e ao gestor (painel de administração, sem
/// necessidade de abrir cada produto individualmente).
/// </summary>
public sealed record ListUnavailableProductsQuery : IQuery<UnavailableProductsResponse>;
