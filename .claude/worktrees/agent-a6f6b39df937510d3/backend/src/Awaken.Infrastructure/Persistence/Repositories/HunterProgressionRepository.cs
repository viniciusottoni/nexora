using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class HunterProgressionRepository(AwakenDbContext context) : IHunterProgressionRepository
{
    public async Task<HunterProgression?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.HunterProgressions
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(HunterProgression progression, CancellationToken cancellationToken = default)
    {
        await context.HunterProgressions.AddAsync(progression, cancellationToken);
    }
}
