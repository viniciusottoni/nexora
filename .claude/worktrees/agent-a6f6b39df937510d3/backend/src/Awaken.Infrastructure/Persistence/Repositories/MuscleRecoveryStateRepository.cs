using Awaken.Domain.Entities.Training;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

/// US-239: tabela pequena por usuário × grupo muscular — sem cache Redis
/// (mesmo padrão de <see cref="TrainingProgramSplitRepository"/>).
public class MuscleRecoveryStateRepository(AwakenDbContext context) : IMuscleRecoveryStateRepository
{
    public async Task<IReadOnlyList<MuscleRecoveryState>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await context.MuscleRecoveryStates
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);

    public async Task<MuscleRecoveryState?> GetByUserAndMuscleGroupAsync(Guid userId, string muscleGroup, CancellationToken ct = default) =>
        await context.MuscleRecoveryStates
            .FirstOrDefaultAsync(s => s.UserId == userId && s.MuscleGroup == muscleGroup, ct);

    public async Task AddAsync(MuscleRecoveryState state, CancellationToken ct = default) =>
        await context.MuscleRecoveryStates.AddAsync(state, ct);

    public void Update(MuscleRecoveryState state) => context.MuscleRecoveryStates.Update(state);
}
