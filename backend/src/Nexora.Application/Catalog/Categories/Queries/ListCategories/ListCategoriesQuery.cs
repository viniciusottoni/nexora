using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Categories.Queries.ListCategories;

/// <summary>
/// Porta de <c>GET /v1/catalog/categories</c> — lista todas as categorias do tenant autenticado
/// (ativas e inativas, para que a tela de gestão permita reativar). O cardápio público
/// (<c>GET /v1/public/menu</c>) usa <c>GetPublicMenuQuery</c>, que só devolve as ativas.
/// </summary>
public sealed record ListCategoriesQuery : IQuery<CategoryListResponse>;
