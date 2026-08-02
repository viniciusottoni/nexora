using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Notifications;

public class NotificationPreference : BaseEntity
{
    public Guid UserId { get; private set; }
    public bool PushEnabled { get; private set; }
    public string? PushToken { get; private set; }
    public string PermissionStatus { get; private set; } = "not_requested";

    // US-092: campos de agendamento e rate-limiting de notificacoes.
    public TimeOnly? PreferredReminderTime { get; private set; }
    public string? Timezone { get; private set; }
    public DateTime? LastNotificationSentAt { get; private set; }
    public int DailyNotificationCount { get; private set; }
    public DateOnly? LastNotificationCountResetDate { get; private set; }

    // US-124: controle de frequencia baixa para comunicacao de reativacao.
    public DateTime? LastReactivationNotificationSentAt { get; private set; }

    private NotificationPreference() { }

    public static NotificationPreference Create(Guid userId, bool pushEnabled, string? pushToken, string permissionStatus, DateTime utcNow) =>
        new()
        {
            UserId = userId,
            PushEnabled = pushEnabled,
            PushToken = pushEnabled ? pushToken : null,
            PermissionStatus = permissionStatus,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
        };

    public void Update(bool pushEnabled, string? pushToken, string permissionStatus, DateTime utcNow)
    {
        PushEnabled = pushEnabled;
        PushToken = pushEnabled ? pushToken : null;
        PermissionStatus = permissionStatus;
        UpdatedAtUtc = utcNow;
    }

    public void UpdateReminderTime(TimeOnly preferredTime, string timezone, DateTime utcNow)
    {
        PreferredReminderTime = preferredTime;
        Timezone = timezone;
        UpdatedAtUtc = utcNow;
    }

    /// US-092 RN-001/RN-004: verifica se o usuario pode receber notificacao hoje
    /// (push habilitado, token presente e limite diario nao atingido).
    public bool CanReceiveNotificationToday(DateOnly today, int maxPerDay = 3)
    {
        if (!PushEnabled || PushToken is null) return false;
        if (LastNotificationCountResetDate != today) return true; // contador ainda nao foi inicializado hoje
        return DailyNotificationCount < maxPerDay;
    }

    /// US-092: registra envio, incrementando contador diario e atualizando timestamp.
    public void RecordNotificationSent(DateTime utcNow)
    {
        var today = DateOnly.FromDateTime(utcNow);
        if (LastNotificationCountResetDate != today)
        {
            DailyNotificationCount = 0;
            LastNotificationCountResetDate = today;
        }
        DailyNotificationCount++;
        LastNotificationSentAt = utcNow;
        UpdatedAtUtc = utcNow;
    }

    /// US-124: verifica se pode receber comunicacao de reativacao (baixa frequencia).
    public bool CanReceiveReactivationNotification(DateTime utcNow, int minDaysBetweenSends = 7)
    {
        if (!PushEnabled || PushToken is null) return false;
        if (!LastReactivationNotificationSentAt.HasValue) return true;
        return (utcNow - LastReactivationNotificationSentAt.Value).TotalDays >= minDaysBetweenSends;
    }

    /// US-124: registra envio de comunicacao de reativacao.
    public void RecordReactivationNotificationSent(DateTime utcNow)
    {
        LastReactivationNotificationSentAt = utcNow;
        UpdatedAtUtc = utcNow;
    }
}
