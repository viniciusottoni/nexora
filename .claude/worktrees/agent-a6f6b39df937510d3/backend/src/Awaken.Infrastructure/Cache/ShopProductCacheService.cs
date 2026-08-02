using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Shop;

namespace Awaken.Infrastructure.Cache;

public class ShopProductCacheService(ICacheService cacheService) : IShopProductCacheService
{
    internal const string ProductsCacheKey = "shop-products:active:v1";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    public Task<IReadOnlyList<ShopProduct>?> GetActiveProductsAsync(CancellationToken ct = default) =>
        cacheService.GetAsync<IReadOnlyList<ShopProduct>>(ProductsCacheKey, ct);

    public Task SetActiveProductsAsync(IReadOnlyList<ShopProduct> products, CancellationToken ct = default) =>
        cacheService.SetAsync(ProductsCacheKey, products, Ttl, ct);

    public Task InvalidateActiveProductsAsync(CancellationToken ct = default) =>
        cacheService.RemoveAsync(ProductsCacheKey, ct);
}
