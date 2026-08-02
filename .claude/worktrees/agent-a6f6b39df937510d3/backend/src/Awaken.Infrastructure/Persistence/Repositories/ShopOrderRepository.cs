using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class ShopOrderRepository(AwakenDbContext context) : IShopOrderRepository
{
    public async Task<ShopOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.ShopOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IEnumerable<ShopOrder>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.ShopOrders.ToListAsync(cancellationToken);

    public async Task AddAsync(ShopOrder entity, CancellationToken cancellationToken = default) =>
        await context.ShopOrders.AddAsync(entity, cancellationToken);

    public void Update(ShopOrder entity) =>
        context.ShopOrders.Update(entity);

    public void Remove(ShopOrder entity) =>
        context.ShopOrders.Remove(entity);

    public async Task<ShopOrder?> GetByExternalTransactionIdAsync(
        string externalTransactionId, CancellationToken ct = default) =>
        await context.ShopOrders
            .FirstOrDefaultAsync(o => o.ExternalTransactionId == externalTransactionId, ct);

    public async Task<IReadOnlyList<ShopOrder>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await context.ShopOrders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync(ct);

    /// US-192: retorna pedidos de compra de um usuario paginados, ordem desc.
    public async Task<(IReadOnlyList<ShopOrder> Items, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.ShopOrders.Where(o => o.UserId == userId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    /// US-193: listagem paginada administrativa com filtros opcionais.
    public async Task<(IReadOnlyList<ShopOrder> Items, int TotalCount)> GetPagedByFilterAsync(
        Guid? userId,
        string? status,
        string? channel,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.ShopOrders.AsQueryable();

        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(channel))
            query = query.Where(o => o.Channel == channel);

        if (dateFrom.HasValue)
            query = query.Where(o => o.CreatedAtUtc >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(o => o.CreatedAtUtc <= dateTo.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
