using Awaken.Application.Common.Interfaces;
using StackExchange.Redis;

namespace Awaken.Infrastructure.Services;

public class LoginAttemptTracker(IConnectionMultiplexer redis) : ILoginAttemptTracker
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(10);

    public async Task<bool> IsLockedOutAsync(string email, CancellationToken ct = default)
    {
        if (!redis.IsConnected)
            return false;

        try
        {
            var db = redis.GetDatabase();
            return await db.KeyExistsAsync(LockoutKey(email));
        }
        catch (RedisException)
        {
            // If Redis is down, fail open instead of breaking authentication.
            return false;
        }
    }

    public async Task RecordFailureAsync(string email, CancellationToken ct = default)
    {
        if (!redis.IsConnected)
            return;

        try
        {
            var db = redis.GetDatabase();
            var countKey = CountKey(email);
            var count = await db.StringIncrementAsync(countKey);

            if (count == 1)
                await db.KeyExpireAsync(countKey, AttemptWindow);

            if (count >= MaxAttempts)
            {
                await db.StringSetAsync(LockoutKey(email), "1", LockoutDuration);
                await db.KeyDeleteAsync(countKey);
            }
        }
        catch (RedisException)
        {
            // Best-effort protection only. If Redis is unavailable, avoid blocking login.
        }
    }

    public async Task ClearAsync(string email, CancellationToken ct = default)
    {
        if (!redis.IsConnected)
            return;

        try
        {
            var db = redis.GetDatabase();
            await db.KeyDeleteAsync(CountKey(email));
            await db.KeyDeleteAsync(LockoutKey(email));
        }
        catch (RedisException)
        {
            // Nothing to clear if Redis is unavailable.
        }
    }

    private static string NormalizeEmail(string email) => email.ToLowerInvariant().Trim();
    private static string CountKey(string email) => $"login-attempts:{NormalizeEmail(email)}";
    private static string LockoutKey(string email) => $"login-lockout:{NormalizeEmail(email)}";
}
