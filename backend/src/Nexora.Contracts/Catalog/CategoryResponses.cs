namespace Nexora.Contracts.Catalog;

/// <summary>Categoria do cardápio, com a contagem de produtos vinculados para a UI.</summary>
public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    short Position,
    bool IsActive,
    int ProductCount);

public sealed record CategoryListResponse(IReadOnlyList<CategoryResponse> Items);
