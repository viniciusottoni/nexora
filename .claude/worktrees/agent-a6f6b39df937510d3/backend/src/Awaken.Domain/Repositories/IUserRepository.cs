using Awaken.Domain.Common;
using Awaken.Domain.Entities.Auth;

namespace Awaken.Domain.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByProviderAsync(AuthProvider provider, string providerUserId, CancellationToken cancellationToken = default);
}
