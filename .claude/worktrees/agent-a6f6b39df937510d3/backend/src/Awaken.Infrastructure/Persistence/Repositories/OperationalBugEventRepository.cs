using Awaken.Domain.Entities.Bugs;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class OperationalBugEventRepository(AwakenDbContext context) : IOperationalBugEventRepository
{
    public async Task AddAsync(OperationalBugEvent evt, CancellationToken ct = default) =>
        await context.OperationalBugEvents.AddAsync(evt, ct);

    public async Task<IReadOnlyList<OperationalBugEvent>> GetByBugIdAsync(Guid bugId, CancellationToken ct = default) =>
        await context.OperationalBugEvents.Where(e => e.BugId == bugId).OrderBy(e => e.CreatedAtUtc).ToListAsync(ct);
}
