using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Catalog.Products.Commands.ReorderProducts;

/// <summary>
/// Reordena (arrastar e soltar) os produtos dentro de uma categoria do tenant autenticado — a
/// nova posição vira <c>SortOrder</c> de cada produto e se reflete no cardápio da mesa e do
/// delivery (US-010 §4, cenário "Ordenação do cardápio"). Porta de
/// <c>PATCH /v1/catalog/products/reorder</c>.
/// </summary>
public sealed record ReorderProductsCommand(Guid CategoryId, IReadOnlyList<Guid> Order) : ICommand;
