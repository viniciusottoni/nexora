using Awaken.Domain.Entities.Bugs;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class OperationalBugRepository(AwakenDbContext context) : IOperationalBugRepository
{
    public async Task AddAsync(OperationalBug bug, CancellationToken ct = default) =>
        await context.OperationalBugs.AddAsync(bug, ct);

    public async Task<OperationalBug?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.OperationalBugs.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, ct);

    public async Task<(IReadOnlyList<OperationalBug> Items, int Total)> GetPagedAsync(
        string? severity, string? status, string? component, string? environment, string? origin,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.OperationalBugs.Where(b => !b.IsDeleted);

        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(b => b.Severity == severity);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(b => b.Status == status);

        if (!string.IsNullOrWhiteSpace(component))
            query = query.Where(b => b.Component == component);

        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(b => b.Environment == environment);

        if (!string.IsNullOrWhiteSpace(origin))
            query = query.Where(b => b.Origin == origin);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(b => b.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
