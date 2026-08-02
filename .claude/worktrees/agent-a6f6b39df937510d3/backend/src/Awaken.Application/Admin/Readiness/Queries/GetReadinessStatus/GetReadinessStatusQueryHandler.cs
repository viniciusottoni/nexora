using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Readiness;
using MediatR;

namespace Awaken.Application.Admin.Readiness.Queries.GetReadinessStatus;

/// <summary>
/// US-218: handler que delega ao IReadinessCheckService a avaliação de readiness por ambiente.
/// </summary>
public class GetReadinessStatusQueryHandler(IReadinessCheckService readinessCheckService)
    : IRequestHandler<GetReadinessStatusQuery, ReadinessStatusResponse>
{
    public Task<ReadinessStatusResponse> Handle(GetReadinessStatusQuery request, CancellationToken cancellationToken) =>
        readinessCheckService.GetReadinessStatusAsync(cancellationToken);
}
