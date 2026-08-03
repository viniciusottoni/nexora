using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Catalog.Variants;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Products.Queries.GetPublicMenu;

/// <summary>
/// Núcleo de montagem do cardápio público (categorias/produtos ativos + menor preço vigente por
/// canal) — reaproveitado por <see cref="GetPublicMenuQueryHandler"/> (nuvem, tenant resolvido pelo
/// domínio customizado, US-010 §7) e por
/// <see cref="GetLocalPublicMenu.GetLocalPublicMenuQueryHandler"/> (edge, tenant fixo da instalação,
/// US-021 §7 <c>GET /v1/public/menu?channel=DINE_IN</c>). A ÚNICA diferença entre os dois
/// consumidores é COMO o tenant é descoberto — a consulta em si (que categorias/produtos entram,
/// como o preço "a partir de" é calculado) é idêntica, e duplicá-la seria o tipo de divergência que
/// o ADR-013 (proibição de código por tenant/canal) existe para evitar.
/// </summary>
internal static class PublicMenuBuilder
{
    public static async Task<PublicMenuResponse> BuildAsync(
        IApplicationDbContext db,
        Guid tenantId,
        string tenantName,
        string? requestedChannel,
        CancellationToken cancellationToken)
    {
        // Canal inválido no cardápio público não é um erro para o cliente final — cai no padrão
        // DineIn (mesmo padrão de ChannelParser usado por CreateVariant/SetVariantPrice, US-011).
        var channel = Channel.DineIn;
        if (ChannelParser.TryParse(requestedChannel, out var parsedChannel))
        {
            channel = parsedChannel;
        }

        var categories = await db.Categories
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.DeletedAt == null && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.SortOrder,
                Products = db.Products
                    .Where(p => p.CategoryId == c.Id && p.DeletedAt == null && p.IsActive && p.IsAvailable)
                    .OrderBy(p => p.SortOrder)
                    .ThenBy(p => p.Name)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Description,
                        p.IngredientsText,
                        p.Allergens,
                        p.SortOrder,
                        ImageUrl = db.MediaAssets
                            .Where(m => m.OwnerType == "PRODUCT" && m.OwnerId == p.Id)
                            .OrderByDescending(m => m.CreatedAt)
                            .Select(m => m.Url)
                            .FirstOrDefault(),
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var productIds = categories.SelectMany(category => category.Products).Select(product => product.Id).ToList();
        var currentPrices = await (
                from variant in db.ProductVariants.AsNoTracking()
                join price in db.Prices.AsNoTracking() on variant.Id equals price.VariantId
                where productIds.Contains(variant.ProductId)
                      && variant.DeletedAt == null
                      && variant.IsActive
                      && price.ValidTo == null
                select new
                {
                    variant.ProductId,
                    Price = new PublicMenuCurrentPrice(variant.Id, price.Channel, price.Amount)
                })
            .ToListAsync(cancellationToken);

        var fromPriceByProduct = currentPrices
            .GroupBy(row => row.ProductId)
            .ToDictionary(
                group => group.Key,
                group => PublicMenuPriceResolver.ResolveFromPrice(channel, group.Select(row => row.Price).ToList()));

        return new PublicMenuResponse(
            tenantId,
            tenantName,
            categories
                .Select(c => new PublicMenuCategoryResponse(
                    c.Id,
                    c.Name,
                    c.Description,
                    c.SortOrder,
                    c.Products
                        .Select(p => new PublicMenuProductResponse(
                            p.Id,
                            p.Name,
                            p.Description,
                            p.IngredientsText,
                            p.Allergens,
                            p.ImageUrl,
                            p.SortOrder,
                            fromPriceByProduct.GetValueOrDefault(p.Id)))
                        .ToList()))
                .ToList());
    }
}
