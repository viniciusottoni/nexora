using Awaken.Domain.Common;
using Awaken.Domain.Entities.Nutrition;

namespace Awaken.Domain.Repositories;

public interface INutritionLogRepository : IRepository<NutritionLog>
{
    Task<NutritionLog?> GetByUserIdAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);
}
