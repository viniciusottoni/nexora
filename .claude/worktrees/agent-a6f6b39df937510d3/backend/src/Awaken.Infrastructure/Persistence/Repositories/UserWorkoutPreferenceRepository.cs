using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class UserWorkoutPreferenceRepository(AwakenDbContext context) : IUserWorkoutPreferenceRepository
{
    public async Task<UserWorkoutPreference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.UserWorkoutPreferences.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IEnumerable<UserWorkoutPreference>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.UserWorkoutPreferences.ToListAsync(cancellationToken);

    public async Task AddAsync(UserWorkoutPreference entity, CancellationToken cancellationToken = default) =>
        await context.UserWorkoutPreferences.AddAsync(entity, cancellationToken);

    public void Update(UserWorkoutPreference entity) => context.UserWorkoutPreferences.Update(entity);

    public void Remove(UserWorkoutPreference entity) => context.UserWorkoutPreferences.Remove(entity);

    public async Task<UserWorkoutPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.UserWorkoutPreferences.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
}
