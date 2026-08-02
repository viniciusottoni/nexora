using Nexora.Domain.Catalog;

namespace Nexora.Application.Catalog.Products.Queries.GetPublicMenu;

public sealed record PublicMenuCurrentPrice(Guid VariantId, Channel Channel, decimal Amount);

public static class PublicMenuPriceResolver
{
    public static decimal? ResolveFromPrice(
        Channel channel,
        IReadOnlyCollection<PublicMenuCurrentPrice> currentPrices)
    {
        var resolved = currentPrices
            .GroupBy(price => price.VariantId)
            .Select(variantPrices =>
                variantPrices.FirstOrDefault(price => price.Channel == channel)?.Amount
                ?? (channel == Channel.DineIn
                    ? null
                    : variantPrices.FirstOrDefault(price => price.Channel == Channel.DineIn)?.Amount))
            .Where(amount => amount.HasValue)
            .ToList();

        return resolved.Count == 0 ? null : resolved.Min();
    }
}
