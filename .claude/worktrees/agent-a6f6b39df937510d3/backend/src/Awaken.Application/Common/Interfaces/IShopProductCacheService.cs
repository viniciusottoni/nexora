namespace Awaken.Application.Common.Interfaces;

public interface IShopProductCacheService
{
    Task InvalidateActiveProductsAsync(CancellationToken ct = default);
}
