using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Branding;
using Nexora.Application.Catalog.Variants;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Catalog;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Products.Queries.GetPublicMenu;

/// <summary>
/// Cardápio público de um estabelecimento — usado pelo cardápio da mesa/PWA/delivery antes de
/// qualquer login. <c>category</c>/<c>product</c> têm RLS (ADR-004): sem
/// <c>ICurrentTenantContext.TenantId</c> resolvido (este endpoint é <c>[AllowAnonymous]</c>), o
/// banco negaria qualquer leitura por padrão. Por isso o tenant é resolvido primeiro por
/// <see cref="Tenant"/>/<see cref="TenantConfig"/> (tabelas de plataforma, fora do RLS — mesmo
/// caminho de <c>GetPublicBrandingQueryHandler</c>) e só então o contexto é fixado explicitamente
/// via <see cref="IApplicationDbContext.SetTenantContextAsync"/> — o mesmo mecanismo que
/// <c>LoginWithPasswordCommandHandler</c> usa para o trecho de fluxo em que o tenant já é
/// conhecido, mas <c>ICurrentTenantContext</c> ainda não está autenticado.
/// </summary>
internal sealed class GetPublicMenuQueryHandler : IRequestHandler<GetPublicMenuQuery, Result<PublicMenuResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetPublicMenuQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PublicMenuResponse>> Handle(GetPublicMenuQuery request, CancellationToken cancellationToken)
    {
        var host = BrandingHost.Normalize(request.Host);

        // Canal inválido no cardápio público não é um erro para o cliente final — cai no padrão
        // DineIn (mesmo padrão de ChannelParser usado por CreateVariant/SetVariantPrice, US-011),
        // em vez de recusar a consulta pública inteira por um parâmetro que hoje só existe "para
        // compatibilidade futura" (ver docstring de GetPublicMenuQuery).
        var channel = Channel.DineIn;
        if (ChannelParser.TryParse(request.Channel, out var requestedChannel))
        {
            channel = requestedChannel;
        }

        var tenant = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.DeletedAt == null && t.Domain != null && t.Domain.ToLower() == host)
            .Select(t => new { t.Id, t.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null)
        {
            return Result<PublicMenuResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.PublicMenuTenantNotFound);
        }

        await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.TenantId == tenant.Id && c.DeletedAt == null && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.SortOrder,
                Products = _db.Products
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
                        ImageUrl = _db.MediaAssets
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
                from variant in _db.ProductVariants.AsNoTracking()
                join price in _db.Prices.AsNoTracking() on variant.Id equals price.VariantId
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

        var response = new PublicMenuResponse(
            tenant.Id,
            tenant.Name,
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

        return Result<PublicMenuResponse>.Success(response);
    }
}
