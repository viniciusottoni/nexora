using Awaken.Domain.Common;
using Awaken.Domain.Entities.Exercises;

namespace Awaken.Domain.Repositories;

public interface IExerciseRawImportRepository : IRepository<ExerciseRawImport>
{
    Task<bool> ExistsByProviderExerciseIdAsync(
        string providerName,
        string providerExerciseId,
        CancellationToken cancellationToken = default);

    Task<ExerciseRawImport?> GetByProviderExerciseIdAsync(
        string providerName,
        string providerExerciseId,
        CancellationToken cancellationToken = default);
}
