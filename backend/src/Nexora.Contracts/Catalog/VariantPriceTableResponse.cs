using System.Text.Json.Serialization;

namespace Nexora.Contracts.Catalog;

/// <summary>
/// Uma linha da tabela de preço por canal de uma variante (US-014 §7/§10). <see cref="Amount"/> é
/// nulo somente no caso defensivo em que nem o canal nem o canal base (<c>DineIn</c>) têm preço
/// vigente algum (variante recém-criada sem nenhum <c>Price</c> ainda). <see cref="IsInherited"/>
/// indica que o canal não tem preço próprio e o valor exibido é herdado do preço vigente de
/// <c>DineIn</c> (US-014 §3.1, cenário Gherkin "Herança do preço base").
/// </summary>
public sealed record VariantChannelPriceRow(
    string Channel,
    [property: JsonConverter(typeof(NullableMoneyJsonConverter))] decimal? Amount,
    bool IsInherited,
    DateTimeOffset? ValidFrom);

/// <summary>
/// Tabela de preço por canal de uma variante — resposta de <c>GET /v1/catalog/variants/{id}/prices</c>
/// (US-014 §10, "Tabela de preços por canal editável em linha"). Sempre traz as quatro linhas de
/// <c>Channel</c> (<c>DineIn</c>/<c>Delivery</c>/<c>Takeout</c>/<c>Marketplace</c>), com ou sem
/// preço próprio.
/// </summary>
public sealed record VariantPriceTableResponse(
    Guid VariantId,
    Guid ProductId,
    IReadOnlyList<VariantChannelPriceRow> Channels);
