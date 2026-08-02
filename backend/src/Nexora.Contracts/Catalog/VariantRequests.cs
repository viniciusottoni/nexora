using System.Text.Json.Serialization;

namespace Nexora.Contracts.Catalog;

/// <summary>
/// Corpo de <c>POST /v1/catalog/products/:productId/variants</c> (US-011 §7). Cria a variante e,
/// na mesma chamada, o preço base (canal <see cref="Channel"/>, padrão <c>DineIn</c> quando nulo) —
/// esta história só entrega um preço "base" por variante; a tabela completa de preço por canal
/// (edição por canal, ajuste em massa, auditoria dedicada) é escopo da US-014.
/// </summary>
public sealed record CreateVariantRequest(
    string Name,
    string? SizeCode,
    string? Sku,
    short? PrepMinutes,
    bool IsDefault,
    [property: JsonConverter(typeof(MoneyJsonConverter))] decimal BasePrice,
    string? Channel);

/// <summary>
/// Corpo de <c>PATCH /v1/catalog/variants/:id</c> — nome, SKU e <c>sizeCode</c>. Deliberadamente
/// não inclui tempo de preparo/limiares (US-016) nem ativação/desativação/"variante padrão"
/// (endpoints dedicados <c>POST .../activate</c>, <c>.../deactivate</c>, <c>.../mark-default</c>),
/// mesmo padrão de <see cref="UpdateProductRequest"/> (US-010).
/// </summary>
public sealed record UpdateVariantRequest(string Name, string? SizeCode, string? Sku);
