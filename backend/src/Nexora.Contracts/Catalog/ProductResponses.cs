namespace Nexora.Contracts.Catalog;

/// <summary>
/// Produto do cardápio para a tela de gestão (web-admin). <see cref="ImageUrl"/> é nulo quando o
/// produto não tem foto — o cardápio exibe um marcador visual neutro nesse caso (US-010 §4,
/// cenário "Produto sem foto"), nunca um erro nem uma imagem genérica inventada.
/// </summary>
public sealed record ProductResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    Guid? StationId,
    string? StationName,
    string Name,
    string? Description,
    string? IngredientsText,
    IReadOnlyList<string> Allergens,
    string? ImageUrl,
    short Position,
    bool IsActive,
    bool IsAvailable,
    bool AllowsFractions,
    short MaxFractions);

public sealed record ProductListResponse(IReadOnlyList<ProductResponse> Items);

/// <summary>Resposta de <c>POST /v1/catalog/products/:id/image</c> — URL de upload direto e a URL pública final (ainda não confirmada).</summary>
public sealed record PrepareProductImageUploadResponse(string UploadUrl, string PublicUrl, DateTimeOffset ExpiresAt);

/// <summary>Resposta de <c>POST /v1/catalog/products/:id/image/confirm</c> — o <c>MediaAsset</c> registrado.</summary>
public sealed record ProductImageResponse(Guid MediaAssetId, string Url);
