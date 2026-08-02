using Awaken.Domain.Entities.Training;

namespace Awaken.Domain.Repositories;

/// US-239: estado de recuperação muscular por usuário × grupo, atualizado ao
/// concluir um treino (RN-008) e lido pelo <see cref="Awaken.Domain.Services.Training.RecoveryPlanner"/>.
public interface IMuscleRecoveryStateRepository
{
    Task<IReadOnlyList<MuscleRecoveryState>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<MuscleRecoveryState?> GetByUserAndMuscleGroupAsync(Guid userId, string muscleGroup, CancellationToken ct = default);

    Task AddAsync(MuscleRecoveryState state, CancellationToken ct = default);

    void Update(MuscleRecoveryState state);
}
