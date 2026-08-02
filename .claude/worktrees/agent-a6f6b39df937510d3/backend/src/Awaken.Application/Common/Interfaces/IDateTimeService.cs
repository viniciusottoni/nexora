namespace Awaken.Application.Common.Interfaces;

public interface IDateTimeService
{
    DateTime UtcNow { get; }
    DateOnly TodayUtc { get; }
}
