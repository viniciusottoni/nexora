using Awaken.Domain.Entities.Admin;

namespace Awaken.Domain.Repositories;

public interface IAdminUserRepository
{
    Task AddAsync(AdminUser adminUser, CancellationToken ct = default);
    Task<AdminUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AdminUser>> GetAllAsync(CancellationToken ct = default);
}
