using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class GoldWalletRepository(AwakenDbContext context) : IGoldWalletRepository
{
    public async Task<GoldWallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.GoldWallets.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<IEnumerable<GoldWallet>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.GoldWallets.ToListAsync(cancellationToken);

    public async Task AddAsync(GoldWallet entity, CancellationToken cancellationToken = default) =>
        await context.GoldWallets.AddAsync(entity, cancellationToken);

    public void Update(GoldWallet entity) => context.GoldWallets.Update(entity);

    public void Remove(GoldWallet entity) => context.GoldWallets.Remove(entity);

    public async Task<GoldWallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.GoldWallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

    public Task ReloadAsync(GoldWallet wallet, CancellationToken cancellationToken = default) =>
        context.Entry(wallet).ReloadAsync(cancellationToken);
}
