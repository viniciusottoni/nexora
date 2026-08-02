using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Catalog.Categories.Commands.ReorderCategories;

/// <summary>
/// Reordena (arrastar e soltar) as categorias do cardápio do tenant autenticado — a nova posição
/// (índice na lista <see cref="Order"/>) vira <c>SortOrder</c> de cada categoria e se reflete em
/// todos os canais (US-010 §4, cenário "Ordenação do cardápio"). Porta de
/// <c>PATCH /v1/catalog/categories/reorder</c>.
/// </summary>
public sealed record ReorderCategoriesCommand(IReadOnlyList<Guid> Order) : ICommand;
