using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Notifications;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Notifications.Commands.UpdateReminderTime;

public class UpdateReminderTimeCommandHandler(
    INotificationPreferenceRepository notificationPreferenceRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<UpdateReminderTimeCommandHandler> logger)
    : IRequestHandler<UpdateReminderTimeCommand, NotificationPreferencesResponse>
{
    public async Task<NotificationPreferencesResponse> Handle(
        UpdateReminderTimeCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var utcNow = dateTimeService.UtcNow;

        logger.LogInformation(
            "Updating reminder time for user {UserId} to {Time} in timezone {Timezone}",
            userId,
            request.PreferredReminderTime,
            request.Timezone);

        var preference = await notificationPreferenceRepository.GetByUserIdAsync(userId, cancellationToken);

        if (preference is null)
        {
            preference = NotificationPreference.Create(userId, false, null, "not_requested", utcNow);
            await notificationPreferenceRepository.AddAsync(preference, cancellationToken);
        }
        else
        {
            notificationPreferenceRepository.Update(preference);
        }

        preference.UpdateReminderTime(request.PreferredReminderTime, request.Timezone, utcNow);

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
