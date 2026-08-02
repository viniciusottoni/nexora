using Awaken.Application.Notifications.Commands.SendStreakRiskAlert;
using MediatR;

namespace Awaken.Infrastructure.Jobs;

/// US-093: gatilho (job recorrente Hangfire) que aciona o alerta de streak em risco.
public class StreakRiskAlertJob(IMediator mediator)
{
    public Task RunAsync(CancellationToken cancellationToken) =>
        mediator.Send(new SendStreakRiskAlertCommand(), cancellationToken);
}
