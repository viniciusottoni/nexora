using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Notifications;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Notifications.Commands.UpdatePreferences;

public class UpdateNotificationPreferencesCommandHandler(
    INotificationPreferenceRepository notificationPreferenceRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<UpdateNotificationPreferencesCommandHandler> logger)
    : IRequestHandler<UpdateNotificationPreferencesCommand, NotificationPreferencesResponse>
{
    private const string DefaultPermissionStatus = "not_requested";

    public async Task<NotificationPreferencesResponse> Handle(
        UpdateNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var utcNow = dateTimeService.UtcNow;
        var permissionStatus = request.PermissionStatus ?? DefaultPermissionStatus;

        logger.LogInformation(
            "Updating notification preferences for user {UserId}: PushEnabled={PushEnabled}, PermissionStatus={PermissionStatus}",
            userId,
            request.PushEnabled,
            permissionStatus);

        var existing = await notificationPreferenceRepository.GetByUserIdAsync(userId, cancellationToken);

        NotificationPreference preference;
        if (existing is null)
        {
            preference = NotificationPreference.Create(userId, request.PushEnabled, request.PushToken, permissionStatus, utcNow);
            await notificationPreferenceRepository.AddAsync(preference, cancellationToken);
        }
        else
        {
            existing.Update(request.PushEnabled, request.PushToken, permissionStatus, utcNow);
            notificationPreferenceRepository.Update(existing);
            preference = existing;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

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
