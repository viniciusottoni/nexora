using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class RankScoreLogRepository(AwakenDbContext context) : IRankScoreLogRepository
{
    public async Task<RankScoreLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.RankScoreLogs.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IEnumerable<RankScoreLog>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.RankScoreLogs.ToListAsync(cancellationToken);

    public async Task AddAsync(RankScoreLog entity, CancellationToken cancellationToken = default) =>
        await context.RankScoreLogs.AddAsync(entity, cancellationToken);

    public void Update(RankScoreLog entity) => context.RankScoreLogs.Update(entity);

    public void Remove(RankScoreLog entity) => context.RankScoreLogs.Remove(entity);
}
