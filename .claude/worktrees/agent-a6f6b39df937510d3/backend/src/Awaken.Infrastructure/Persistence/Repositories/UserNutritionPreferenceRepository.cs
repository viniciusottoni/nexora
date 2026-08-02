using Awaken.Domain.Entities.Nutrition;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class UserNutritionPreferenceRepository(AwakenDbContext context) : IUserNutritionPreferenceRepository
{
    public async Task<UserNutritionPreference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.UserNutritionPreferences.FindAsync([id], cancellationToken);

    public async Task<IEnumerable<UserNutritionPreference>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.UserNutritionPreferences.ToListAsync(cancellationToken);

    public async Task AddAsync(UserNutritionPreference entity, CancellationToken cancellationToken = default)
        => await context.UserNutritionPreferences.AddAsync(entity, cancellationToken);

    public void Update(UserNutritionPreference entity)
        => context.UserNutritionPreferences.Update(entity);

    public void Remove(UserNutritionPreference entity)
        => context.UserNutritionPreferences.Remove(entity);

    public async Task<UserNutritionPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.UserNutritionPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
}
