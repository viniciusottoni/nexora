using MediatR;

namespace Awaken.Application.Notifications.Commands.SendReactivationNotification;

public record SendReactivationNotificationCommand() : IRequest<SendReactivationNotificationResult>;

public record SendReactivationNotificationResult(int Eligible, int Sent, int Skipped);
