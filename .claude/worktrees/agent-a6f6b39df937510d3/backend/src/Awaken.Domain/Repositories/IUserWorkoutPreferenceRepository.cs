using Awaken.Domain.Common;
using Awaken.Domain.Entities.Onboarding;

namespace Awaken.Domain.Repositories;

public interface IUserWorkoutPreferenceRepository : IRepository<UserWorkoutPreference>
{
    Task<UserWorkoutPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
