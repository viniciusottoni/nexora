using Awaken.Domain.Entities.Support;

namespace Awaken.Domain.Repositories;

public interface ISupportTicketRepository
{
    Task AddAsync(SupportTicket ticket, CancellationToken ct);
    Task<(IReadOnlyList<SupportTicket> Items, int Total)> GetPagedAsync(string? status, string? priority, string? category, int page, int pageSize, CancellationToken ct = default);
    Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
