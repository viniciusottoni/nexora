using Awaken.Domain.Entities.Progression;

namespace Awaken.Domain.Repositories;

public interface IHunterProgressionRepository
{
    Task<HunterProgression?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(HunterProgression progression, CancellationToken cancellationToken = default);
}
