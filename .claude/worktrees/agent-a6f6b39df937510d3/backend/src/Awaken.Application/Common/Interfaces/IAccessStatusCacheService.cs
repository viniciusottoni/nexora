namespace Awaken.Application.Common.Interfaces;

public interface IAccessStatusCacheService
{
    Task<string?> GetAsync(Guid userId, CancellationToken ct = default);
    Task SetAsync(Guid userId, string accessStatus, CancellationToken ct = default);
    Task InvalidateAsync(Guid userId, CancellationToken ct = default);
}
