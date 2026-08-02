using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class GoldLedgerEntryRepository(AwakenDbContext context) : IGoldLedgerEntryRepository
{
    public async Task<GoldLedgerEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.GoldLedgerEntries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IEnumerable<GoldLedgerEntry>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.GoldLedgerEntries.ToListAsync(cancellationToken);

    public async Task AddAsync(GoldLedgerEntry entity, CancellationToken cancellationToken = default) =>
        await context.GoldLedgerEntries.AddAsync(entity, cancellationToken);

    public void Update(GoldLedgerEntry entity) => context.GoldLedgerEntries.Update(entity);

    public void Remove(GoldLedgerEntry entity) => context.GoldLedgerEntries.Remove(entity);

    public async Task<IReadOnlyList<GoldLedgerEntry>> GetByWalletIdAsync(
        Guid walletId, CancellationToken cancellationToken = default) =>
        await context.GoldLedgerEntries
            .Where(e => e.WalletId == walletId)
            .OrderBy(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// US-192: retorna lancamentos paginados de uma carteira, ordem desc.
    /// </summary>
    public async Task<(IReadOnlyList<GoldLedgerEntry> Items, int TotalCount)> GetPagedByWalletIdAsync(
        Guid walletId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.GoldLedgerEntries
            .Where(e => e.WalletId == walletId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>
    /// US-229: listagem admin paginada com filtros opcionais, ordem desc.
    /// </summary>
    public async Task<(IReadOnlyList<GoldLedgerEntry> Items, int TotalCount)> GetAdminPagedAsync(
        Guid? walletId,
        string? direction,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        GoldLedgerDirection? dir = direction switch
        {
            "credit" => GoldLedgerDirection.Credit,
            "debit"  => GoldLedgerDirection.Debit,
            _        => null,
        };

        var query = context.GoldLedgerEntries.AsQueryable();
        if (walletId.HasValue)  query = query.Where(e => e.WalletId == walletId.Value);
        if (dir.HasValue)       query = query.Where(e => e.Direction == dir.Value);
        if (dateFrom.HasValue)  query = query.Where(e => e.CreatedAtUtc >= dateFrom.Value);
        if (dateTo.HasValue)    query = query.Where(e => e.CreatedAtUtc <= dateTo.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
