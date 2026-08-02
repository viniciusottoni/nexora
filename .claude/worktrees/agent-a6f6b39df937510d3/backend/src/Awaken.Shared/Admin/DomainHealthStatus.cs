namespace Awaken.Shared.Admin;

/// <summary>
/// EPIC-017 (US-216, US-218, US-220, US-221, US-222): shared status vocabulary for MVP health/readiness
/// domain cards. RN-EPIC-017-018 — a domain without telemetry must report NoData, never Healthy.
/// </summary>
public static class DomainHealthStatus
{
    public const string Healthy = "healthy";
    public const string Attention = "attention";
    public const string Critical = "critical";
    public const string NoData = "no_data";

    /// <summary>Worst-of aggregation: any Critical wins, then Attention, then NoData, else Healthy.</summary>
    public static string Aggregate(IEnumerable<string> statuses)
    {
        var set = statuses.ToHashSet();
        if (set.Contains(Critical)) return Critical;
        if (set.Contains(Attention)) return Attention;
        if (set.Count == 0 || set.All(s => s == NoData)) return NoData;
        return Healthy;
    }
}
