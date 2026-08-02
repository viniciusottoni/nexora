using System.Text.Json;
using Awaken.Application.Common.Interfaces;
using StackExchange.Redis;

namespace Awaken.Infrastructure.Cache;

/// <summary>
/// Cache strategy: Redis is used as a read-through/write-through cache only (ADR-009).
/// It is NEVER used as the source of truth for progression (XP, rank, streak),
/// financial data (subscriptions, IAP), or inventory. All canonical data lives in
/// PostgreSQL. Redis caches are always expired or invalidated, never the final arbiter.
/// </summary>
public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private readonly IConnectionMultiplexer _redis = redis;
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_redis.IsConnected)
            return default;

        try
        {
            var value = await _db.StringGetAsync(key);
            if (value.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>((string)value!);
        }
        catch (RedisException)
        {
            // Cache is best-effort only. If Redis is unavailable, fall back to DB-backed reads.
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        if (!_redis.IsConnected)
            return;

        try
        {
            var serialized = JsonSerializer.Serialize(value);
            if (expiry.HasValue)
                await _db.StringSetAsync(key, serialized, expiry.Value);
            else
                await _db.StringSetAsync(key, serialized);
        }
        catch (RedisException)
        {
            // Cache write failure must not break the request path.
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_redis.IsConnected)
            return;

        try
        {
            await _db.KeyDeleteAsync(key);
        }
        catch (RedisException)
        {
            // Cache eviction is best-effort only.
        }
    }
}
