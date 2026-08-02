using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class NotificationLogRepository(AwakenDbContext context) : INotificationLogRepository
{
    public async Task<NotificationLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.NotificationLogs.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IEnumerable<NotificationLog>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.NotificationLogs.ToListAsync(cancellationToken);

    public async Task AddAsync(NotificationLog entity, CancellationToken cancellationToken = default) =>
        await context.NotificationLogs.AddAsync(entity, cancellationToken);

    public void Update(NotificationLog entity) => context.NotificationLogs.Update(entity);

    public void Remove(NotificationLog entity) => context.NotificationLogs.Remove(entity);

    public async Task<List<NotificationLog>> GetTodayByUserIdAsync(
        Guid userId,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var startOfDay = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endOfDay = today.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        return await context.NotificationLogs
            .Where(l => l.UserId == userId
                        && l.AttemptedAtUtc >= startOfDay
                        && l.AttemptedAtUtc <= endOfDay)
            .ToListAsync(cancellationToken);
    }
}
