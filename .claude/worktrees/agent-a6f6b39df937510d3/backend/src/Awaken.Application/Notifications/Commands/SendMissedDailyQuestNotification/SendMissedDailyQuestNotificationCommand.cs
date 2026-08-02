using MediatR;

namespace Awaken.Application.Notifications.Commands.SendMissedDailyQuestNotification;

public record SendMissedDailyQuestNotificationCommand() : IRequest<SendMissedDailyQuestNotificationResult>;

public record SendMissedDailyQuestNotificationResult(int Eligible, int Sent, int Skipped);
