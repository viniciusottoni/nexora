using MediatR;

namespace Awaken.Application.Notifications.Commands.EvaluateNotification;

public record EvaluateNotificationCommand(Guid UserId, string NotificationType)
    : IRequest<EvaluateNotificationResult>;

public record EvaluateNotificationResult(bool Allowed, string? BlockReason, Guid LogId);
