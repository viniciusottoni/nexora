using Awaken.Domain.Entities.Shop;

namespace Awaken.Domain.Repositories;

public interface IShopProductRepository
{
    Task<IReadOnlyList<ShopProduct>> GetActiveProductsAsync(CancellationToken ct);
    Task<ShopProduct?> GetByKeyAsync(string key, CancellationToken ct);
}
