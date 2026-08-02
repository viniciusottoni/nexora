using Awaken.Domain.Common;
using Awaken.Domain.Entities.Auth;

namespace Awaken.Domain.Repositories;

public interface IPasswordResetRepository : IRepository<PasswordResetRequest>
{
    Task<PasswordResetRequest?> GetActiveByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);
}
