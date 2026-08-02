using Awaken.Domain.Entities.Nutrition;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class NutritionLogRepository(AwakenDbContext context) : INutritionLogRepository
{
    public async Task<NutritionLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.NutritionLogs.FindAsync([id], cancellationToken);

    public async Task<IEnumerable<NutritionLog>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.NutritionLogs.ToListAsync(cancellationToken);

    public async Task AddAsync(NutritionLog entity, CancellationToken cancellationToken = default)
        => await context.NutritionLogs.AddAsync(entity, cancellationToken);

    public void Update(NutritionLog entity)
        => context.NutritionLogs.Update(entity);

    public void Remove(NutritionLog entity)
        => context.NutritionLogs.Remove(entity);

    public async Task<NutritionLog?> GetByUserIdAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default)
        => await context.NutritionLogs
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Date == date, cancellationToken);
}
