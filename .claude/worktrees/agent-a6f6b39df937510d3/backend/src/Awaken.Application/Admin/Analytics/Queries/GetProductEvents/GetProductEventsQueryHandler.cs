using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Analytics;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Analytics.Queries.GetProductEvents;

/// <summary>
/// US-168 — eventos do produto por volume/distribuição.
///
/// RN-004: eventos sem volume recente ainda devem aparecer explicitamente
/// (Volume=0, HasNoRecentVolume=true) em vez de serem omitidos silenciosamente.
/// A taxonomia base é a união de todas as Actions já registradas (ActorType=User)
/// em qualquer momento com as que ocorreram no período consultado.
/// </summary>
public class GetProductEventsQueryHandler(
    IAdminAnalyticsRepository repository,
    IDateTimeService dateTimeService)
    : IRequestHandler<GetProductEventsQuery, ProductEventsResponse>
{
    public async Task<ProductEventsResponse> Handle(GetProductEventsQuery request, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeService.UtcNow;
        var from = request.From ?? utcNow.Date.AddDays(-7);
        var to = request.To ?? utcNow;

        var taxonomy = await repository.GetAllDistinctUserActionsAsync(cancellationToken);
        var periodVolumes = await repository.GetEventVolumesAsync(from, to, request.EventNameFilter, cancellationToken);

        var volumeByAction = periodVolumes.ToDictionary(v => v.Action, v => v.Count);

        // RN-004: taxonomia base = união entre todas as actions conhecidas e as observadas
        // no período (esta última cobertura adicional caso o filtro de nome restrinja a taxonomia).
        var allActions = taxonomy
            .Union(periodVolumes.Select(v => v.Action))
            .Where(a => string.IsNullOrWhiteSpace(request.EventNameFilter)
                        || a.Contains(request.EventNameFilter, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .OrderBy(a => a)
            .ToList();

        var events = allActions
            .Select(action =>
            {
                var hasVolume = volumeByAction.TryGetValue(action, out var count);
                return new ProductEventItem(action, hasVolume ? count : 0, !hasVolume || count == 0);
            })
            .ToList();

        var timeSeriesRows = await repository.GetEventTimeSeriesAsync(from, to, allActions, cancellationToken);
        var timeSeries = timeSeriesRows
            .Select(r => new EventTimeSeriesPoint(r.Date, r.Action, r.Count))
            .ToList();

        return new ProductEventsResponse(events, timeSeries);
    }
}
