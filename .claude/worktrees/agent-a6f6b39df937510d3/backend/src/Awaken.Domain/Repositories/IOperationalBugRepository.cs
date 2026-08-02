using Awaken.Domain.Entities.Bugs;

namespace Awaken.Domain.Repositories;

public interface IOperationalBugRepository
{
    Task AddAsync(OperationalBug bug, CancellationToken ct = default);
    Task<OperationalBug?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<OperationalBug> Items, int Total)> GetPagedAsync(string? severity, string? status, string? component, string? environment, string? origin, int page, int pageSize, CancellationToken ct = default);
}
