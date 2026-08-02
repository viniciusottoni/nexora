using Awaken.Domain.Entities.Support;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class SupportTicketRepository(AwakenDbContext context) : ISupportTicketRepository
{
    public async Task AddAsync(SupportTicket ticket, CancellationToken ct) =>
        await context.SupportTickets.AddAsync(ticket, ct);

    public async Task<(IReadOnlyList<SupportTicket> Items, int Total)> GetPagedAsync(
        string? status, string? priority, string? category,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.SupportTickets.Where(t => !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(t => t.Priority == priority);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(t => t.Category == category);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.SupportTickets.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);
}
