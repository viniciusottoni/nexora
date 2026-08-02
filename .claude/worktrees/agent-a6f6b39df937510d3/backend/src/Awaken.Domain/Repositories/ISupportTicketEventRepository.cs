using Awaken.Domain.Entities.Support;

namespace Awaken.Domain.Repositories;

public interface ISupportTicketEventRepository
{
    Task AddAsync(SupportTicketEvent evt, CancellationToken ct = default);
    Task<IReadOnlyList<SupportTicketEvent>> GetByTicketIdAsync(Guid ticketId, CancellationToken ct = default);
}
