using Awaken.Domain.Common;
using Awaken.Domain.Entities.Notifications;

namespace Awaken.Domain.Repositories;

public interface INotificationPreferenceRepository : IRepository<NotificationPreference>
{
    Task<NotificationPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// US-092: retorna todas as preferencias com push habilitado e token presente.
    Task<List<NotificationPreference>> GetAllWithPushEnabledAsync(CancellationToken cancellationToken = default);

    /// US-207: retorna uma pagina de preferencias com push habilitado, ordenadas por Id (cursor-based).
    Task<List<NotificationPreference>> GetPageWithPushEnabledAsync(
        Guid? afterId,
        int pageSize,
        CancellationToken cancellationToken = default);
}
