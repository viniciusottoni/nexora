using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Products.Queries.ListProducts;

/// <summary>
/// Porta de <c>GET /v1/catalog/products</c> — lista os produtos do tenant autenticado (ativos e
/// inativos). <see cref="CategoryId"/> opcional filtra por categoria (usado pela tela de gestão
/// ao navegar por categoria e pelo drag-and-drop de reordenação, que só precisa dos produtos de
/// uma categoria por vez).
/// </summary>
public sealed record ListProductsQuery(Guid? CategoryId) : IQuery<ProductListResponse>;
