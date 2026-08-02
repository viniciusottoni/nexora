using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class UserRepository(AwakenDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Users.ToListAsync(cancellationToken);

    public async Task AddAsync(User entity, CancellationToken cancellationToken = default) =>
        await context.Users.AddAsync(entity, cancellationToken);

    public void Update(User entity) => context.Users.Update(entity);

    public void Remove(User entity) => context.Users.Remove(entity);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await context.Users.FirstOrDefaultAsync(
            u => u.Email == email.ToLowerInvariant() && !u.IsDeleted, cancellationToken);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await context.Users.AnyAsync(
            u => u.Email == email.ToLowerInvariant() && !u.IsDeleted, cancellationToken);

    public async Task<User?> GetByProviderAsync(AuthProvider provider, string providerUserId, CancellationToken cancellationToken = default) =>
        await context.Users.FirstOrDefaultAsync(
            u => u.Provider == provider && u.ProviderUserId == providerUserId && !u.IsDeleted,
            cancellationToken);
}
