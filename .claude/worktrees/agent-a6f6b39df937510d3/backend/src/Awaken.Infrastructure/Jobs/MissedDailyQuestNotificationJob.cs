using Awaken.Application.Notifications.Commands.SendMissedDailyQuestNotification;
using MediatR;

namespace Awaken.Infrastructure.Jobs;

/// US-135: gatilho recorrente (Hangfire) que aciona o aviso de quest diária perdida com penalidade aplicada.
public class MissedDailyQuestNotificationJob(IMediator mediator)
{
    public Task RunAsync(CancellationToken cancellationToken) =>
        mediator.Send(new SendMissedDailyQuestNotificationCommand(), cancellationToken);
}
