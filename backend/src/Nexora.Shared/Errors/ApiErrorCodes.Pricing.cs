namespace Nexora.Shared.Errors;

/// <summary>
/// Códigos de erro do módulo de preço por canal de venda (US-014, ADR-021).
/// </summary>
/// <remarks>
/// NOTA DE INTEGRAÇÃO: no momento em que este arquivo foi escrito, o worktree ainda não tinha
/// nenhum <c>ApiErrorCodes.Catalog.cs</c> (US-010/US-011 — categorias, produtos e variantes —
/// ainda não tinham camada de Application/Contracts/Api.Cloud aqui, só o <c>Nexora.Domain</c> e a
/// persistência via EF Core já existiam). Por isso os códigos abaixo são deliberadamente
/// autocontidos (prefixo <c>PRICE_TABLE_*</c>/<c>PRICE_BULK_*</c>) em vez de reaproveitar nomes
/// como <c>VariantNotFound</c>/<c>PriceChannelInvalid</c> que a US-011 provavelmente também
/// define — evita colisão de nome de membro (erro de compilação em C#, já que
/// <c>ApiErrorCodes</c> é uma única <c>partial class</c>) quando a implementação "de verdade" da
/// US-011 for mesclada por cima deste trabalho paralelo. Se, na mesclagem, os códigos desta US-014
/// se mostrarem redundantes com os de US-011, é seguro consolidá-los — nenhum outro código deste
/// módulo depende do nome exato, só do valor de string (que também é distinto por segurança).
/// </remarks>
public static partial class ApiErrorCodes
{
    /// <summary>Variante referenciada em <c>GET/PUT /v1/catalog/variants/{id}/prices</c> não existe ou não pertence ao tenant.</summary>
    public const string PriceTableVariantNotFound = "PRICE_TABLE_VARIANT_NOT_FOUND";

    /// <summary>Categoria referenciada em <c>POST /v1/catalog/prices/bulk-adjust</c> não existe ou não pertence ao tenant.</summary>
    public const string PriceTableCategoryNotFound = "PRICE_TABLE_CATEGORY_NOT_FOUND";

    /// <summary>Canal de venda enviado não corresponde a um valor válido de <c>Channel</c> (DineIn/Delivery/Takeout/Marketplace).</summary>
    public const string PriceTableChannelInvalid = "PRICE_TABLE_CHANNEL_INVALID";

    /// <summary>
    /// <c>PUT /v1/catalog/variants/{id}/prices</c> enviou o mesmo canal mais de uma vez na mesma
    /// chamada — cada canal só pode ser definido uma vez por requisição (senão a ordem de
    /// fechamento/criação das linhas de <c>Price</c> ficaria ambígua).
    /// </summary>
    public const string PriceTableChannelDuplicated = "PRICE_TABLE_CHANNEL_DUPLICATED";

    /// <summary>
    /// <c>POST /v1/catalog/prices/bulk-adjust</c> resultaria em preço negativo para ao menos uma
    /// variação ativa da categoria — a operação inteira é recusada antes de qualquer
    /// <c>SaveChangesAsync</c> (nenhuma variação é alterada), preservando a garantia "reajuste em
    /// massa é transacional, falha parcial não deixa preços inconsistentes" (US-014 §12).
    /// </summary>
    public const string PriceBulkAdjustNegativeResult = "PRICE_BULK_ADJUST_NEGATIVE_RESULT";
}
