using Awaken.Application.Progression.Commands.ApplyDailyQuestPenalties;
using MediatR;

namespace Awaken.Infrastructure.Jobs;

/// US-129: gatilho de virada de dia (job recorrente Hangfire) que aciona a penalidade de XP.
public class DailyQuestPenaltyJob(IMediator mediator)
{
    public Task RunAsync(CancellationToken cancellationToken) =>
        mediator.Send(new ApplyDailyQuestPenaltiesCommand(), cancellationToken);
}
