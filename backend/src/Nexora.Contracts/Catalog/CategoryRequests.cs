namespace Nexora.Contracts.Catalog;

/// <summary>Corpo de <c>POST /v1/catalog/categories</c> (US-010 §7).</summary>
public sealed record CreateCategoryRequest(string Name, string? Description, short Position);

/// <summary>
/// Corpo de <c>PATCH /v1/catalog/categories/:id</c> — todos os campos opcionais, só o que for
/// enviado é alterado (mesmo padrão de <c>UpdateStationRequest</c>). <see cref="IsActive"/>
/// permite reativar uma categoria desativada pelo mesmo formulário de edição.
/// </summary>
public sealed record UpdateCategoryRequest(string? Name, string? Description, short? Position, bool? IsActive);

/// <summary>Corpo de <c>PATCH /v1/catalog/categories/reorder</c> — nova ordem completa das categorias do tenant.</summary>
public sealed record ReorderCategoriesRequest(IReadOnlyList<Guid> Order);
