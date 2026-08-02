using Awaken.Domain.Entities.Support;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class SupportTicketEventRepository(AwakenDbContext context) : ISupportTicketEventRepository
{
    public async Task AddAsync(SupportTicketEvent evt, CancellationToken ct = default) =>
        await context.SupportTicketEvents.AddAsync(evt, ct);

    public async Task<IReadOnlyList<SupportTicketEvent>> GetByTicketIdAsync(Guid ticketId, CancellationToken ct = default) =>
        await context.SupportTicketEvents.Where(e => e.TicketId == ticketId).OrderBy(e => e.CreatedAtUtc).ToListAsync(ct);
}
