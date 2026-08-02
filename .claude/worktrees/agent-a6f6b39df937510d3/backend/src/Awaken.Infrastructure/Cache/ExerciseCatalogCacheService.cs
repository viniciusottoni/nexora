using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Exercises;

namespace Awaken.Infrastructure.Cache;

public class ExerciseCatalogCacheService(ICacheService cacheService) : IExerciseCatalogCacheService
{
    internal const string CatalogCacheKey = "exercise-catalog:approved:v1";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);

    public Task<IReadOnlyList<ExerciseCatalog>?> GetApprovedCatalogAsync(CancellationToken ct = default) =>
        cacheService.GetAsync<IReadOnlyList<ExerciseCatalog>>(CatalogCacheKey, ct);

    public Task SetApprovedCatalogAsync(IReadOnlyList<ExerciseCatalog> catalog, CancellationToken ct = default) =>
        cacheService.SetAsync(CatalogCacheKey, catalog, Ttl, ct);

    public Task InvalidateApprovedCatalogAsync(CancellationToken ct = default) =>
        cacheService.RemoveAsync(CatalogCacheKey, ct);
}
