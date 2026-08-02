using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Notifications;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Notifications.Queries.GetPreferences;

public class GetNotificationPreferencesQueryHandler(
    INotificationPreferenceRepository notificationPreferenceRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetNotificationPreferencesQuery, NotificationPreferencesResponse?>
{
    public async Task<NotificationPreferencesResponse?> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var preference = await notificationPreferenceRepository.GetByUserIdAsync(userId, cancellationToken);

        if (preference is null)
            return null;

        return new NotificationPreferencesResponse(
            preference.UserId,
            preference.PushEnabled,
            preference.PushEnabled ? preference.PushToken : null,
            preference.PermissionStatus,
            preference.PreferredReminderTime,
            preference.Timezone,
            preference.UpdatedAtUtc);
    }
}
