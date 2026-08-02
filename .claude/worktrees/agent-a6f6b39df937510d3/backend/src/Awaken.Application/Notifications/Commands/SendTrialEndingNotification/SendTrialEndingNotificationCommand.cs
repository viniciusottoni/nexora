using MediatR;

namespace Awaken.Application.Notifications.Commands.SendTrialEndingNotification;

public record SendTrialEndingNotificationCommand() : IRequest<SendTrialEndingNotificationResult>;

public record SendTrialEndingNotificationResult(int Eligible, int Sent, int Skipped);
