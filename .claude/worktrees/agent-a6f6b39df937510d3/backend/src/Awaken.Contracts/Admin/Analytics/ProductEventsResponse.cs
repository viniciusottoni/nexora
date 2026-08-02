namespace Awaken.Contracts.Admin.Analytics;

/// <summary>US-168 — eventos de produto por volume/distribuição.</summary>
public record ProductEventsResponse(
    IReadOnlyList<ProductEventItem> Events,
    IReadOnlyList<EventTimeSeriesPoint> TimeSeries);

public record EventTimeSeriesPoint(DateOnly Date, string EventName, int Count);
