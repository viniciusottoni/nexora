using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Routines;
using MediatR;

namespace Awaken.Application.Admin.Routines.Queries.GetRoutinesOverview;

/// <summary>
/// US-221: handler que delega ao IJobMonitoringService a leitura agregada do sistema de jobs.
/// </summary>
public class GetRoutinesOverviewQueryHandler(IJobMonitoringService jobMonitoringService)
    : IRequestHandler<GetRoutinesOverviewQuery, RoutinesOverviewResponse>
{
    public Task<RoutinesOverviewResponse> Handle(GetRoutinesOverviewQuery request, CancellationToken cancellationToken) =>
        jobMonitoringService.GetRoutinesOverviewAsync(cancellationToken);
}
