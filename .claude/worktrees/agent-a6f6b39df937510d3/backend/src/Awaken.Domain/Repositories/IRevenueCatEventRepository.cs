using Awaken.Domain.Entities.Subscriptions;

namespace Awaken.Domain.Repositories;

/// <summary>
/// US-194: idempotency store for RevenueCat webhook events.
/// </summary>
public interface IRevenueCatEventRepository
{
    Task<bool> ExistsByEventIdAsync(string eventId, CancellationToken cancellationToken = default);
    Task AddAsync(RevenueCatEvent revenueCatEvent, CancellationToken cancellationToken = default);
}
