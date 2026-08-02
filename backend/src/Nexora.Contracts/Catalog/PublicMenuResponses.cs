using System.Text.Json.Serialization;

namespace Nexora.Contracts.Catalog;

/// <summary>
/// Produto ativo exibido no cardápio público de um canal (mesa, delivery etc.) — US-010 §7,
/// <c>GET /v1/public/menu</c>. <see cref="FromPrice"/> (US-011 §4, cenário "Produto com três
/// tamanhos": "o preço exibido inicialmente deve ser o da menor variação, com indicação 'a partir
/// de'") é o menor preço vigente, no canal consultado, entre as variações ativas do produto — nulo
/// quando nenhuma variação tem preço definido nesse canal ainda.
/// </summary>
public sealed record PublicMenuProductResponse(
    Guid Id,
    string Name,
    string? Description,
    string? IngredientsText,
    IReadOnlyList<string> Allergens,
    string? ImageUrl,
    short Position,
    [property: JsonConverter(typeof(NullableMoneyJsonConverter))] decimal? FromPrice);

public sealed record PublicMenuCategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    short Position,
    IReadOnlyList<PublicMenuProductResponse> Products);

/// <summary>
/// Cardápio público completo de um estabelecimento, só com categorias/produtos ativos. Tenant
/// resolvido pelo domínio customizado (parâmetro <c>host</c>), mesmo mecanismo de
/// <c>GET /public/branding</c> (US-003) — não existe hoje outro jeito de identificar o tenant em
/// endpoint sem autenticação (ver nota em <c>GetPublicMenuQueryHandler</c>).
/// </summary>
public sealed record PublicMenuResponse(
    Guid TenantId,
    string TenantName,
    IReadOnlyList<PublicMenuCategoryResponse> Categories);
