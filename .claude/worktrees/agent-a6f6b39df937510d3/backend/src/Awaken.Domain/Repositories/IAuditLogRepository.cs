using Awaken.Domain.Entities.Audit;

namespace Awaken.Domain.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog entry, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AuditLog> Items, int Total)> GetPagedAsync(string? actorType, string? action, string? resourceType, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default);
    Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
