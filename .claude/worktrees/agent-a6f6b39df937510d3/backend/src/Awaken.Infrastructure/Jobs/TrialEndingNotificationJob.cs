using Awaken.Application.Notifications.Commands.SendTrialEndingNotification;
using MediatR;

namespace Awaken.Infrastructure.Jobs;

/// US-123: gatilho recorrente (Hangfire) que aciona o envio de avisos de fim de trial.
public class TrialEndingNotificationJob(IMediator mediator)
{
    public Task RunAsync(CancellationToken cancellationToken) =>
        mediator.Send(new SendTrialEndingNotificationCommand(), cancellationToken);
}
