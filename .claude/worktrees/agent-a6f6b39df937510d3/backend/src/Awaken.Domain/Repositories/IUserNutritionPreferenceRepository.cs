using Awaken.Domain.Common;
using Awaken.Domain.Entities.Nutrition;

namespace Awaken.Domain.Repositories;

public interface IUserNutritionPreferenceRepository : IRepository<UserNutritionPreference>
{
    Task<UserNutritionPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
