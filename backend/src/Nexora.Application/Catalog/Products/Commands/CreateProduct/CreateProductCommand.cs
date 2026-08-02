using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Products.Commands.CreateProduct;

/// <summary>Cria um produto do cardápio no tenant autenticado. Porta de <c>POST /v1/catalog/products</c>.</summary>
public sealed record CreateProductCommand(
    Guid CategoryId,
    string Name,
    Guid? StationId,
    string? Description,
    string? IngredientsText,
    IReadOnlyList<string>? Allergens,
    bool AllowsFractions,
    short MaxFractions,
    short Position,
    bool IsActive) : ICommand<ProductResponse>;
