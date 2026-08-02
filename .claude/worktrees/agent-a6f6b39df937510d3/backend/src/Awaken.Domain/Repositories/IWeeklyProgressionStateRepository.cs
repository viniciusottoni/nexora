using Awaken.Domain.Entities.Progression;

namespace Awaken.Domain.Repositories;

/// US-241: estado de progressão semanal por usuário, lido/escrito pelo
/// <see cref="Awaken.Application.Progression.Common.WeeklyProgressionReviewer"/>.
public interface IWeeklyProgressionStateRepository
{
    Task<WeeklyProgressionState?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(WeeklyProgressionState state, CancellationToken cancellationToken = default);

    void Update(WeeklyProgressionState state);
}
