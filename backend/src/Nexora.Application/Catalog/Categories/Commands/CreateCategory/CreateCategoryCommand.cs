using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Categories.Commands.CreateCategory;

/// <summary>Cria uma categoria do cardápio no tenant autenticado. Porta de <c>POST /v1/catalog/categories</c>.</summary>
public sealed record CreateCategoryCommand(string Name, string? Description, short Position) : ICommand<CategoryResponse>;
