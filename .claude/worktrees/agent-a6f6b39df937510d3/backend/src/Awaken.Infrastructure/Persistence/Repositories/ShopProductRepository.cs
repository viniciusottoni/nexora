using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Repositories;
using Awaken.Infrastructure.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class ShopProductRepository(
    AwakenDbContext context,
    ShopProductCacheService productCache,
    ILogger<ShopProductRepository> logger) : IShopProductRepository
{
    /// US-206: cache-aside pattern — Redis TTL 10 min; any Redis failure falls through to DB.
    public async Task<IReadOnlyList<ShopProduct>> GetActiveProductsAsync(CancellationToken ct)
    {
        try
        {
            var cached = await productCache.GetActiveProductsAsync(ct);
            if (cached is not null)
                return cached;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable for shop products cache; falling back to DB");
        }

        var result = await context.ShopProducts
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Key)
            .ToListAsync(ct);

        try
        {
            await productCache.SetActiveProductsAsync(result, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable when writing shop products cache");
        }

        return result;
    }

    public async Task<ShopProduct?> GetByKeyAsync(string key, CancellationToken ct) =>
        await context.ShopProducts.FirstOrDefaultAsync(p => p.Key == key && p.IsActive, ct);
}
