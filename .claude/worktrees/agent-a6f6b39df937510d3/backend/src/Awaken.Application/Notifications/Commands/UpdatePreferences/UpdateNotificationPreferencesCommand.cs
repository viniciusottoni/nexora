using Awaken.Contracts.Notifications;
using MediatR;

namespace Awaken.Application.Notifications.Commands.UpdatePreferences;

public record UpdateNotificationPreferencesCommand(
    bool PushEnabled,
    string? PushToken,
    string? PermissionStatus) : IRequest<NotificationPreferencesResponse>;
