using Awaken.Domain.Common;
using Awaken.Domain.Entities.Notifications;

namespace Awaken.Domain.Repositories;

public interface INotificationLogRepository : IRepository<NotificationLog>
{
    Task<List<NotificationLog>> GetTodayByUserIdAsync(
        Guid userId,
        DateOnly today,
        CancellationToken cancellationToken = default);
}
