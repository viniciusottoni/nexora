using Awaken.Domain.Entities.Bugs;

namespace Awaken.Domain.Repositories;

public interface IOperationalBugEventRepository
{
    Task AddAsync(OperationalBugEvent evt, CancellationToken ct = default);
    Task<IReadOnlyList<OperationalBugEvent>> GetByBugIdAsync(Guid bugId, CancellationToken ct = default);
}
