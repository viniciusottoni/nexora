using Awaken.Domain.Common;
using Awaken.Domain.Entities.Exercises;

namespace Awaken.Domain.Repositories;

public interface IExerciseCatalogRepository : IRepository<ExerciseCatalog>
{
    Task<ExerciseCatalog?> GetByProviderExerciseIdAsync(
        string providerName,
        string providerExerciseId,
        CancellationToken cancellationToken = default);

    /// US-239: resolve pelo <c>ProviderExerciseId</c> apenas — usado quando só se tem esse
    /// valor à mão (ex.: <c>QuestExercise.ExerciseCatalogProviderId</c>, que não carrega o
    /// nome do provider). Assume unicidade do id entre providers, válido para o pipeline
    /// único de import desta MVP (ver US-236).
    Task<ExerciseCatalog?> GetByProviderExerciseIdAsync(
        string providerExerciseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExerciseCatalog>> ListApprovedForWorkoutGenerationAsync(
        CancellationToken cancellationToken = default);
}
