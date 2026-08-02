namespace Awaken.Application.Common.Interfaces;

public interface IExerciseCatalogCacheService
{
    Task InvalidateApprovedCatalogAsync(CancellationToken ct = default);
}
