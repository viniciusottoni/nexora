using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Notifications;

/// US-095 RN-007: registra cada decisão de envio de notificação para auditoria básica.
public class NotificationLog : BaseEntity
{
    public Guid UserId { get; private set; }
    public string NotificationType { get; private set; } = string.Empty;
    public string DecisionStatus { get; private set; } = string.Empty;
    public string? DecisionReason { get; private set; }
    public DateTime AttemptedAtUtc { get; private set; }

    private NotificationLog() { }

    public static NotificationLog Create(
        Guid userId,
        string notificationType,
        string decisionStatus,
        string? decisionReason,
        DateTime utcNow) =>
        new()
        {
            UserId = userId,
            NotificationType = notificationType,
            DecisionStatus = decisionStatus,
            DecisionReason = decisionReason,
            AttemptedAtUtc = utcNow,
            CreatedAtUtc = utcNow,
        };
}
