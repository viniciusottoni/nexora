using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class QuestLogRepository(AwakenDbContext context) : IQuestLogRepository
{
    public async Task<QuestLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.QuestLogs.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IEnumerable<QuestLog>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.QuestLogs.ToListAsync(cancellationToken);

    public async Task AddAsync(QuestLog entity, CancellationToken cancellationToken = default) =>
        await context.QuestLogs.AddAsync(entity, cancellationToken);

    public void Update(QuestLog entity) => context.QuestLogs.Update(entity);

    public void Remove(QuestLog entity) => context.QuestLogs.Remove(entity);

    public async Task<QuestLog?> GetByQuestIdAsync(Guid questId, CancellationToken cancellationToken = default) =>
        await context.QuestLogs.FirstOrDefaultAsync(l => l.QuestId == questId, cancellationToken);

    public async Task<IReadOnlyList<QuestLog>> GetRecentByUserIdAsync(
        Guid userId, int limit, CancellationToken cancellationToken = default) =>
        await context.QuestLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CompletedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<QuestLog> Items, bool HasMore)> GetPagedByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var raw = await context.QuestLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CompletedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = raw.Count > pageSize;
        if (hasMore) raw.RemoveAt(raw.Count - 1);
        return (raw, hasMore);
    }

    /// US-209: paginacao por cursor; mais estavel que offset sob insercoes concorrentes.
    /// O cursor e o Id do ultimo item retornado. Ordena por (CompletedAtUtc DESC, Id DESC).
    public async Task<(List<QuestLog> Items, bool HasMore)> GetPageByUserIdAsync(
        Guid userId,
        Guid? afterId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var maxLimit = Math.Min(limit, 50);

        IQueryable<QuestLog> query = context.QuestLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CompletedAtUtc)
            .ThenByDescending(l => l.Id);

        if (afterId.HasValue)
        {
            var cursorItem = await context.QuestLogs
                .AsNoTracking()
                .Where(l => l.UserId == userId && l.Id == afterId.Value)
                .Select(l => new { l.CompletedAtUtc, l.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (cursorItem is not null)
            {
                query = query.Where(l =>
                    l.CompletedAtUtc < cursorItem.CompletedAtUtc ||
                    (l.CompletedAtUtc == cursorItem.CompletedAtUtc && l.Id < afterId.Value));
            }
        }

        var items = await query.Take(maxLimit + 1).ToListAsync(cancellationToken);
        var hasMore = items.Count > maxLimit;
        if (hasMore) items.RemoveAt(items.Count - 1);

        return (items, hasMore);
    }

    public async Task<IReadOnlyList<QuestLog>> GetCompletedSinceAsync(
        Guid userId, DateTime sinceUtc, CancellationToken cancellationToken = default) =>
        await context.QuestLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.CompletedAtUtc >= sinceUtc)
            .OrderByDescending(l => l.CompletedAtUtc)
            .ToListAsync(cancellationToken);
}
