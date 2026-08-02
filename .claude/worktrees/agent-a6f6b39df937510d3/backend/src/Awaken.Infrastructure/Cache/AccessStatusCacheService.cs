using Awaken.Application.Common.Interfaces;

namespace Awaken.Infrastructure.Cache;

public class AccessStatusCacheService(ICacheService cacheService) : IAccessStatusCacheService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);
    private static string Key(Guid userId) => $"access-status:{userId}";

    public Task<string?> GetAsync(Guid userId, CancellationToken ct = default) =>
        cacheService.GetAsync<string>(Key(userId), ct);

    public Task SetAsync(Guid userId, string accessStatus, CancellationToken ct = default) =>
        cacheService.SetAsync(Key(userId), accessStatus, Ttl, ct);

    public Task InvalidateAsync(Guid userId, CancellationToken ct = default) =>
        cacheService.RemoveAsync(Key(userId), ct);
}
