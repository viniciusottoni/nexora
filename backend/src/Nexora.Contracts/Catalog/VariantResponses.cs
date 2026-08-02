using System.Text.Json.Serialization;

namespace Nexora.Contracts.Catalog;

/// <summary>
/// Variante de produto para a tela de gestão (web-admin) — inclui o preço vigente no canal
/// consultado (<see cref="CurrentPriceChannel"/>, padrão <c>DineIn</c>) quando existir. Preço por
/// canal completo (todos os canais simultaneamente, ajuste em massa) é escopo da US-014; aqui só
/// o preço base de um canal é exposto, suficiente para US-011.
/// </summary>
public sealed record VariantResponse(
    Guid Id,
    Guid ProductId,
    string Name,
    string? Sku,
    string? SizeCode,
    short PrepMinutes,
    bool IsDefault,
    bool IsActive,
    [property: JsonConverter(typeof(NullableMoneyJsonConverter))] decimal? CurrentPrice,
    string? CurrentPriceChannel);

public sealed record VariantListResponse(IReadOnlyList<VariantResponse> Items);
