using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Categories.Commands.UpdateCategory;

/// <summary>Atualiza uma categoria do cardápio do tenant autenticado. Porta de <c>PATCH /v1/catalog/categories/:id</c>.</summary>
public sealed record UpdateCategoryCommand(
    Guid CategoryId,
    string? Name,
    string? Description,
    short? Position,
    bool? IsActive) : ICommand<CategoryResponse>;
