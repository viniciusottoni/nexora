using Nexora.Domain.Catalog;

namespace Nexora.Application.Catalog.Prices.Queries.ListVariantPricesByChannel;

/// <summary>Canal já resolvido — traz o preço próprio do canal ou, na ausência dele, o preço herdado de <see cref="Channel.DineIn"/>.</summary>
public sealed record ResolvedChannelPrice(
    Channel Channel,
    decimal? Amount,
    bool IsInherited,
    DateTimeOffset? ValidFrom,
    Guid? SourcePriceId);

/// <summary>
/// Regra de herança de preço por canal (US-014 §3.1, cenário Gherkin "Herança do preço base": "Dado
/// uma variação sem preço específico para o canal de balcão, quando um pedido de balcão for
/// criado, então deve ser aplicado o preço base da variação"). Função pura, sem I/O — recebe só os
/// preços VIGENTES já carregados do banco (<c>ValidTo == null</c>) e devolve, para cada canal, o
/// preço próprio quando existir ou o preço vigente de <see cref="Channel.DineIn"/> como base.
/// Usada por <see cref="ListVariantPricesByChannelQueryHandler"/> (que carrega os preços vigentes
/// do banco) e coberta isoladamente, sem banco nenhum, por
/// <c>Nexora.UnitTests.Catalog.PriceChannelInheritanceTests</c>.
/// </summary>
public static class ChannelPriceResolver
{
    /// <summary>Os quatro canais de venda, na ordem em que a tabela de preço é exibida (US-014 §10).</summary>
    public static readonly IReadOnlyList<Channel> AllChannels = new[]
    {
        Channel.DineIn,
        Channel.Delivery,
        Channel.Takeout,
        Channel.Marketplace,
    };

    /// <summary>
    /// Resolve um único canal contra o conjunto de preços vigentes de uma variante (no máximo um
    /// por canal — <paramref name="currentPrices"/> já deve estar filtrado a <c>ValidTo == null</c>).
    /// </summary>
    public static ResolvedChannelPrice Resolve(Channel channel, IReadOnlyCollection<Price> currentPrices)
    {
        var own = currentPrices.FirstOrDefault(p => p.Channel == channel);
        if (own is not null)
        {
            return new ResolvedChannelPrice(channel, own.Amount, IsInherited: false, own.ValidFrom, own.Id);
        }

        // DineIn é o próprio canal-base: sem preço próprio nele, não há de onde herdar (caso
        // defensivo — toda variante nasce com um preço em algum canal, mas o modelo não impede
        // que esse canal inicial seja outro que não DineIn, US-011 permite escolher o canal na
        // criação).
        if (channel == Channel.DineIn)
        {
            return new ResolvedChannelPrice(channel, Amount: null, IsInherited: false, ValidFrom: null, SourcePriceId: null);
        }

        var basePrice = currentPrices.FirstOrDefault(p => p.Channel == Channel.DineIn);
        return basePrice is null
            ? new ResolvedChannelPrice(channel, Amount: null, IsInherited: false, ValidFrom: null, SourcePriceId: null)
            : new ResolvedChannelPrice(channel, basePrice.Amount, IsInherited: true, basePrice.ValidFrom, basePrice.Id);
    }

    /// <summary>Resolve os quatro canais (<see cref="AllChannels"/>) de uma só vez, na ordem de exibição da tabela.</summary>
    public static IReadOnlyList<ResolvedChannelPrice> ResolveAll(IReadOnlyCollection<Price> currentPrices) =>
        AllChannels.Select(channel => Resolve(channel, currentPrices)).ToList();
}
