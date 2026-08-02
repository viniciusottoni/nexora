using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class NotificationPreferenceRepository(AwakenDbContext context) : INotificationPreferenceRepository
{
    public async Task<NotificationPreference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.NotificationPreferences.FirstOrDefaultAsync(np => np.Id == id, cancellationToken);

    public async Task<IEnumerable<NotificationPreference>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.NotificationPreferences.ToListAsync(cancellationToken);

    public async Task AddAsync(NotificationPreference entity, CancellationToken cancellationToken = default) =>
        await context.NotificationPreferences.AddAsync(entity, cancellationToken);

    public void Update(NotificationPreference entity) => context.NotificationPreferences.Update(entity);

    public void Remove(NotificationPreference entity) => context.NotificationPreferences.Remove(entity);

    public async Task<NotificationPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.NotificationPreferences.FirstOrDefaultAsync(np => np.UserId == userId, cancellationToken);

    /// US-092: retorna preferencias com push habilitado e token presente para o envio de lembretes.
    public async Task<List<NotificationPreference>> GetAllWithPushEnabledAsync(CancellationToken cancellationToken = default) =>
        await context.NotificationPreferences
            .AsNoTracking()
            .Where(np => np.PushEnabled && np.PushToken != null)
            .ToListAsync(cancellationToken);

    /// US-207: pagina de preferencias com push habilitado, ordenadas por Id para paginacao por cursor.
    public async Task<List<NotificationPreference>> GetPageWithPushEnabledAsync(
        Guid? afterId,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<NotificationPreference> query = context.NotificationPreferences
            .AsNoTracking()
            .Where(p => p.PushEnabled && p.PushToken != null)
            .OrderBy(p => p.Id);

        if (afterId.HasValue)
            query = query.Where(p => p.Id > afterId.Value);

        return await query.Take(pageSize).ToListAsync(cancellationToken);
    }
}
