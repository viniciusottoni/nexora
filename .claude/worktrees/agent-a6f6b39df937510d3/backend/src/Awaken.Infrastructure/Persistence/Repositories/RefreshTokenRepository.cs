using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository(AwakenDbContext context, IDateTimeService dateTimeService)
    : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Id == id, cancellationToken);

    public async Task<IEnumerable<RefreshToken>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.RefreshTokens.ToListAsync(cancellationToken);

    public async Task AddAsync(RefreshToken entity, CancellationToken cancellationToken = default) =>
        await context.RefreshTokens.AddAsync(entity, cancellationToken);

    public void Update(RefreshToken entity) => context.RefreshTokens.Update(entity);

    public void Remove(RefreshToken entity) => context.RefreshTokens.Remove(entity);

    public async Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default
    ) =>
        await context.RefreshTokens.FirstOrDefaultAsync(
            rt => rt.TokenHash == tokenHash,
            cancellationToken
        );

    public async Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);
        var utcNow = dateTimeService.UtcNow;
        foreach (var token in tokens)
            token.Revoke(utcNow);
    }
}
