using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Prices.Queries.ListVariantPricesByChannel;

/// <summary>
/// Traz, para uma variação, o preço vigente de cada um dos quatro canais de venda — aplicando a
/// herança do preço base (<see cref="ChannelPriceResolver"/>) quando o canal não tem preço próprio
/// (US-014 §3.1/§10). Porta de <c>GET /v1/catalog/variants/{id}/prices</c>.
/// </summary>
public sealed record ListVariantPricesByChannelQuery(Guid VariantId) : IQuery<VariantPriceTableResponse>;
