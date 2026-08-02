using Awaken.Domain.Entities.Training;

namespace Awaken.Domain.Repositories;

public interface ITrainingProgramRepository
{
    Task<IReadOnlyList<TrainingProgram>> GetActiveProgramsAsync(CancellationToken ct = default);
    Task<TrainingProgram?> GetByKeyAsync(string key, CancellationToken ct = default);
}
