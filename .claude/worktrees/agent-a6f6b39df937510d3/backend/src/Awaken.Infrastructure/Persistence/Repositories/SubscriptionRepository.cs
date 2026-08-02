using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class SubscriptionRepository(AwakenDbContext context) : ISubscriptionRepository
{
    public async Task<bool> HasAnySubscriptionRecordAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Subscriptions
            .AnyAsync(s => s.UserId == userId, cancellationToken);
    }

    public async Task<Subscription?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }

    public async Task<Subscription?> GetByRevenueCatCustomerIdAsync(
        string revenueCatCustomerId, CancellationToken cancellationToken = default) =>
        await context.Subscriptions
            .FirstOrDefaultAsync(s => s.RevenueCatCustomerId == revenueCatCustomerId, cancellationToken);

    public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        await context.Subscriptions.AddAsync(subscription, cancellationToken);
    }

    public void Update(Subscription subscription) =>
        context.Subscriptions.Update(subscription);
}
