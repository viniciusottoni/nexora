using System.Globalization;
using Awaken.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Awaken.Infrastructure.Services;

public class UserDateService(IHttpContextAccessor httpContextAccessor, IDateTimeService dateTimeService)
    : IUserDateService
{
    private const string TimezoneOffsetHeader = "X-Timezone-Offset-Minutes";

    public DateOnly TodayLocal
    {
        get
        {
            var offset = ResolveOffset();
            var localNow = new DateTimeOffset(dateTimeService.UtcNow, TimeSpan.Zero).ToOffset(offset);
            return DateOnly.FromDateTime(localNow.DateTime);
        }
    }

    /// <summary>US-088: hora local para cálculo da fração do dia transcorrida.</summary>
    public DateTime NowLocal
    {
        get
        {
            var offset = ResolveOffset();
            return new DateTimeOffset(dateTimeService.UtcNow, TimeSpan.Zero).ToOffset(offset).DateTime;
        }
    }

    private TimeSpan ResolveOffset()
    {
        var rawOffset = httpContextAccessor.HttpContext?.Request.Headers[TimezoneOffsetHeader].FirstOrDefault();
        if (rawOffset is null) return TimeSpan.Zero;

        if (!int.TryParse(rawOffset, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
        {
            return TimeSpan.Zero;
        }

        minutes = Math.Clamp(minutes, -14 * 60, 14 * 60);
        return TimeSpan.FromMinutes(minutes);
    }
}
