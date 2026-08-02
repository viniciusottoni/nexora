using Awaken.Application.Notifications.Commands.SendReactivationNotification;
using MediatR;

namespace Awaken.Infrastructure.Jobs;

/// US-124: gatilho recorrente (Hangfire) que aciona o envio de comunicacoes de reativacao.
public class ReactivationNotificationJob(IMediator mediator)
{
    public Task RunAsync(CancellationToken cancellationToken) =>
        mediator.Send(new SendReactivationNotificationCommand(), cancellationToken);
}
