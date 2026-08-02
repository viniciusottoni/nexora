using Awaken.Application.Common.Interfaces;

namespace Awaken.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => TruncateToMicroseconds(DateTime.UtcNow);
    public DateOnly TodayUtc => DateOnly.FromDateTime(UtcNow);

    // Postgres timestamp precision is microseconds; normalize here so first
    // response and reloaded persisted value stay identical.
    private static DateTime TruncateToMicroseconds(DateTime utcNow)
    {
        var ticks = utcNow.Ticks - (utcNow.Ticks % 10);
        return new DateTime(ticks, DateTimeKind.Utc);
    }
}
