namespace Nexora.Contracts.Catalog;

/// <summary>Corpo de <c>POST /v1/catalog/products</c> (US-010 §7).</summary>
public sealed record CreateProductRequest(
    Guid CategoryId,
    string Name,
    Guid? StationId,
    string? Description,
    string? IngredientsText,
    IReadOnlyList<string>? Allergens,
    bool AllowsFractions,
    short MaxFractions,
    short Position,
    bool IsActive);

/// <summary>
/// Corpo de <c>PATCH /v1/catalog/products/:id</c> — todos os campos opcionais, só o que for
/// enviado é alterado (mesmo padrão de <c>UpdateStationRequest</c>). Ativação/desativação tem
/// endpoints dedicados (<c>POST .../activate</c>, <c>POST .../deactivate</c>) — não é feita aqui,
/// para deixar explícito na auditoria que é uma ação distinta de uma edição de campo (US-010 §3.1:
/// "Ativação e desativação de produto, distinto de indisponibilidade operacional").
/// </summary>
public sealed record UpdateProductRequest(
    string? Name,
    Guid? CategoryId,
    Guid? StationId,
    string? Description,
    string? IngredientsText,
    IReadOnlyList<string>? Allergens,
    bool? AllowsFractions,
    short? MaxFractions,
    short? Position);

/// <summary>Corpo de <c>PATCH /v1/catalog/products/reorder</c> (US-010 §7) — nova ordem dos produtos dentro de uma categoria.</summary>
public sealed record ReorderProductsRequest(Guid CategoryId, IReadOnlyList<Guid> Order);

/// <summary>Corpo de <c>POST /v1/catalog/products/:id/image</c> — prepara upload pré-assinado (US-010 §10).</summary>
public sealed record PrepareProductImageUploadRequest(string ContentType, int Bytes, string Sha256);

/// <summary>
/// Corpo de <c>POST /v1/catalog/products/:id/image/confirm</c> — confirma que o upload direto ao
/// object storage terminou com sucesso e registra o <c>MediaAsset</c> definitivo. Passo adicional
/// deliberado em relação a <c>PrepareBrandingUpload</c> (que já cria o <c>MediaAsset</c> no
/// próprio prepare): sem confirmação explícita, um prepare nunca seguido de upload real deixaria
/// uma foto "fantasma" aparecendo como a mais recente do produto.
/// </summary>
public sealed record ConfirmProductImageRequest(
    string Url,
    string ContentType,
    int Bytes,
    string Sha256,
    int? Width,
    int? Height);
