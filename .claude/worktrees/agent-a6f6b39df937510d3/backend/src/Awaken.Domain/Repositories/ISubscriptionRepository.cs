using Awaken.Domain.Entities.Subscriptions;

namespace Awaken.Domain.Repositories;

public interface ISubscriptionRepository
{
    Task<bool> HasAnySubscriptionRecordAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Subscription?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>US-194: find subscription by RevenueCat AppUserId for webhook event processing.</summary>
    Task<Subscription?> GetByRevenueCatCustomerIdAsync(string revenueCatCustomerId, CancellationToken cancellationToken = default);

    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);
    void Update(Subscription subscription);
}
