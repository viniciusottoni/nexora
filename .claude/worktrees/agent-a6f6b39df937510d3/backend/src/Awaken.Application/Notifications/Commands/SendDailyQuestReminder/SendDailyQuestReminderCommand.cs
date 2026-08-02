using MediatR;

namespace Awaken.Application.Notifications.Commands.SendDailyQuestReminder;

public record SendDailyQuestReminderCommand() : IRequest<SendDailyQuestReminderResult>;

public record SendDailyQuestReminderResult(int Eligible, int Sent, int Skipped);
