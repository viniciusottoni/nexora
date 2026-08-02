namespace Awaken.Contracts.Notifications;

public record UpdateNotificationPreferencesRequest(
    bool PushEnabled,
    string? PushToken,
    string? PermissionStatus);
