namespace Awaken.Application.Common.Interfaces;

public interface ILoginAttemptTracker
{
    Task<bool> IsLockedOutAsync(string email, CancellationToken ct = default);
    Task RecordFailureAsync(string email, CancellationToken ct = default);
    Task ClearAsync(string email, CancellationToken ct = default);
}
