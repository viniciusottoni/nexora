using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Timeline;
using MediatR;

namespace Awaken.Application.Admin.Timeline.Queries.GetOperationalTimeline;

public sealed class GetOperationalTimelineQueryHandler
    : IRequestHandler<GetOperationalTimelineQuery, OperationalTimelineResponse>
{
    private readonly IOperationalTimelineService _timelineService;

    public GetOperationalTimelineQueryHandler(IOperationalTimelineService timelineService)
    {
        _timelineService = timelineService;
    }

    public Task<OperationalTimelineResponse> Handle(
        GetOperationalTimelineQuery request,
        CancellationToken cancellationToken)
        => _timelineService.GetTimelineAsync(request.Filters, cancellationToken);
}
