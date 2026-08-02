using Awaken.Domain.Common;
using Awaken.Domain.Entities.Onboarding;

namespace Awaken.Domain.Repositories;

public interface IUserProfileRepository : IRepository<UserProfile>
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
