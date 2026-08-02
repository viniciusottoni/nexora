namespace Awaken.Contracts.Admin.Timeline;

public record OperationalTimelineResponse(
    IReadOnlyList<TimelineEntryResponse> Entries,
    ImpactSummaryResponse Impact,
    DateTime GeneratedAtUtc);
