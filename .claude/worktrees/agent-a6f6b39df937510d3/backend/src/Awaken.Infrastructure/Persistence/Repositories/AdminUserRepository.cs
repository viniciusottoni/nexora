using Awaken.Domain.Entities.Admin;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class AdminUserRepository(AwakenDbContext context) : IAdminUserRepository
{
    public async Task AddAsync(AdminUser adminUser, CancellationToken ct = default) =>
        await context.AdminUsers.AddAsync(adminUser, ct);

    public async Task<AdminUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await context.AdminUsers.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant() && !u.IsDeleted, ct);

    public async Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.AdminUsers.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);

    public async Task<IReadOnlyList<AdminUser>> GetAllAsync(CancellationToken ct = default) =>
        await context.AdminUsers.Where(u => !u.IsDeleted).ToListAsync(ct);
}
