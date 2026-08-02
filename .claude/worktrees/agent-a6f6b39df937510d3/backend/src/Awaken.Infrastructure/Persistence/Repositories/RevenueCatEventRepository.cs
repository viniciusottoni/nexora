using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

/// <summary>US-194: idempotency store for RevenueCat webhook events.</summary>
public class RevenueCatEventRepository(AwakenDbContext context) : IRevenueCatEventRepository
{
    public async Task<bool> ExistsByEventIdAsync(string eventId, CancellationToken cancellationToken = default) =>
        await context.RevenueCatEvents.AnyAsync(e => e.EventId == eventId, cancellationToken);

    public async Task AddAsync(RevenueCatEvent revenueCatEvent, CancellationToken cancellationToken = default) =>
        await context.RevenueCatEvents.AddAsync(revenueCatEvent, cancellationToken);
}
