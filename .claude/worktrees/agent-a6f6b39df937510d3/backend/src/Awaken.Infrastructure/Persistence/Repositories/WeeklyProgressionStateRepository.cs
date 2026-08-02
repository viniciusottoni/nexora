using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

/// US-241: tabela pequena por usuário — sem cache Redis (mesmo padrão de
/// <see cref="MuscleRecoveryStateRepository"/>).
public class WeeklyProgressionStateRepository(AwakenDbContext context) : IWeeklyProgressionStateRepository
{
    public async Task<WeeklyProgressionState?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.WeeklyProgressionStates.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

    public async Task AddAsync(WeeklyProgressionState state, CancellationToken cancellationToken = default) =>
        await context.WeeklyProgressionStates.AddAsync(state, cancellationToken);

    public void Update(WeeklyProgressionState state) => context.WeeklyProgressionStates.Update(state);
}
