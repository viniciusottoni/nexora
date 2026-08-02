using Awaken.Contracts.Admin.Timeline;

namespace Awaken.Application.Common.Interfaces;

public class OperationalTimelineFilters
{
    public string? Environment { get; init; }
    public string? Severity { get; init; }
    public Guid? UserId { get; init; }
    public string? Resource { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}

public interface IOperationalTimelineService
{
    Task<OperationalTimelineResponse> GetTimelineAsync(OperationalTimelineFilters filters, CancellationToken ct);
}
