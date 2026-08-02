namespace Awaken.Contracts.Notifications;

public record NotificationPreferencesResponse(
    Guid UserId,
    bool PushEnabled,
    string? PushToken,
    string PermissionStatus,
    TimeOnly? PreferredReminderTime,
    string? Timezone,
    DateTime? UpdatedAtUtc);
