using Awaken.Application.Notifications.Commands.SendDailyQuestReminder;
using MediatR;

namespace Awaken.Infrastructure.Jobs;

/// US-092: gatilho recorrente (Hangfire) que aciona o envio de lembretes de quest diaria.
public class DailyQuestReminderJob(IMediator mediator)
{
    public Task RunAsync(CancellationToken cancellationToken) =>
        mediator.Send(new SendDailyQuestReminderCommand(), cancellationToken);
}
