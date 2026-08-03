using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Catalog.Prices.Queries.ListVariantPricesByChannel;
using Nexora.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Orders.Support;

/// <summary>
/// US-030 §4, cenário "Preço aplicado por canal": resolve o preço VIGENTE de uma variante no canal
/// real do pedido (nunca mais fixo em <see cref="Channel.DineIn"/> — esse hard-code era o gap
/// documentado de US-030 em <c>AddOrderItemCommandHandler</c>/<c>RepeatOrderItemCommandHandler</c>,
/// construídos antes desta história existir). Reaproveita
/// <see cref="ChannelPriceResolver"/> (US-014, "canal sem preço próprio herda do DINE_IN") em vez
/// de duplicar a regra de herança — mesmo preço que a tabela de canais da US-014 mostra ao dono é o
/// preço que o pedido efetivamente grava no item.
/// </summary>
public static class OrderItemPriceResolver
{
    public static async Task<decimal?> ResolveAsync(
        IApplicationDbContext db, Guid variantId, Guid tenantId, Channel channel, CancellationToken cancellationToken)
    {
        var currentPrices = await db.Prices
            .Where(p => p.VariantId == variantId && p.TenantId == tenantId && p.ValidTo == null)
            .ToListAsync(cancellationToken);

        return ChannelPriceResolver.Resolve(channel, currentPrices).Amount;
    }
}
