using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class AuditLogRepository(AwakenDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog entry, CancellationToken cancellationToken = default) =>
        await context.AuditLogs.AddAsync(entry, cancellationToken);

    public async Task<(IReadOnlyList<AuditLog> Items, int Total)> GetPagedAsync(
        string? actorType, string? action, string? resourceType,
        DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(actorType) && Enum.TryParse<AuditActorType>(actorType, ignoreCase: true, out var parsedActorType))
            query = query.Where(a => a.ActorType == parsedActorType);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (!string.IsNullOrWhiteSpace(resourceType))
            query = query.Where(a => a.ResourceType == resourceType);

        if (from.HasValue)
            query = query.Where(a => a.CreatedAtUtc >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.CreatedAtUtc <= to.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.AuditLogs.FirstOrDefaultAsync(a => a.Id == id, ct);
}
