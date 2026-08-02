using Nexora.Domain.Catalog;

namespace Nexora.Application.Catalog.Variants;

/// <summary>
/// Mantém a definição de "preço base" da US-011 consistente nos comandos: preço vigente do
/// canal DineIn. Preços dos demais canais pertencem à tabela da US-014.
/// </summary>
public static class VariantBasePriceQuery
{
    public static IQueryable<Price> CurrentDineInFor(this IQueryable<Price> prices, Guid variantId) =>
        prices.Where(price =>
            price.VariantId == variantId &&
            price.Channel == Channel.DineIn &&
            price.ValidTo == null);
}
