using Awaken.Application.Admin.Economy.Commands.ReconcileGoldEconomy;
using MediatR;

namespace Awaken.Infrastructure.Jobs;

/// <summary>
/// US-228: gatilho recorrente (Hangfire) da reconciliação da economia Gold. Apenas orquestra —
/// toda a lógica de detecção de divergência vive em ReconcileGoldEconomyCommandHandler.
/// </summary>
public class GoldEconomyReconciliationJob(IMediator mediator)
{
    public Task RunAsync(CancellationToken cancellationToken) =>
        mediator.Send(new ReconcileGoldEconomyCommand(), cancellationToken);
}
