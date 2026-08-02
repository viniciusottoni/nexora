using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class UserProfileRepository(AwakenDbContext context) : IUserProfileRepository
{
    public async Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.UserProfiles.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IEnumerable<UserProfile>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.UserProfiles.ToListAsync(cancellationToken);

    public async Task AddAsync(UserProfile entity, CancellationToken cancellationToken = default) =>
        await context.UserProfiles.AddAsync(entity, cancellationToken);

    public void Update(UserProfile entity) => context.UserProfiles.Update(entity);

    public void Remove(UserProfile entity) => context.UserProfiles.Remove(entity);

    public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
}
