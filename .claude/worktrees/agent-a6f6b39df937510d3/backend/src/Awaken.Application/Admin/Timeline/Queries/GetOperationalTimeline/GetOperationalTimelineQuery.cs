using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Timeline;
using MediatR;

namespace Awaken.Application.Admin.Timeline.Queries.GetOperationalTimeline;

public record GetOperationalTimelineQuery(OperationalTimelineFilters Filters) : IRequest<OperationalTimelineResponse>;
