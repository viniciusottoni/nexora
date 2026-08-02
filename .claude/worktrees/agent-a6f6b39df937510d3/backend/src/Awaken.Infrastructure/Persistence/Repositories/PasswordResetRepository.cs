using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class PasswordResetRepository(AwakenDbContext context) : IPasswordResetRepository
{
    public async Task<PasswordResetRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.PasswordResetRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IEnumerable<PasswordResetRequest>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.PasswordResetRequests.ToListAsync(cancellationToken);

    public async Task AddAsync(PasswordResetRequest entity, CancellationToken cancellationToken = default) =>
        await context.PasswordResetRequests.AddAsync(entity, cancellationToken);

    public void Update(PasswordResetRequest entity) =>
        context.PasswordResetRequests.Update(entity);

    public void Remove(PasswordResetRequest entity) =>
        context.PasswordResetRequests.Remove(entity);

    public async Task<PasswordResetRequest?> GetActiveByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        await context.PasswordResetRequests
            .FirstOrDefaultAsync(
                r => r.TokenHash == tokenHash && r.UsedAtUtc == null,
                cancellationToken);
}
