using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Repositories;
using Awaken.Infrastructure.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class ExerciseCatalogRepository(
    AwakenDbContext context,
    ExerciseCatalogCacheService catalogCache,
    ILogger<ExerciseCatalogRepository> logger) : IExerciseCatalogRepository
{
    public async Task<ExerciseCatalog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.ExerciseCatalogs
            .Include(e => e.AttributeContribution)
            .Include(e => e.Relations)
            .Include(e => e.Taxonomy)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IEnumerable<ExerciseCatalog>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.ExerciseCatalogs
            .Include(e => e.AttributeContribution)
            .Include(e => e.Relations)
            .Include(e => e.Taxonomy)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ExerciseCatalog entity, CancellationToken cancellationToken = default) =>
        await context.ExerciseCatalogs.AddAsync(entity, cancellationToken);

    public void Update(ExerciseCatalog entity)
    {
        context.Entry(entity).State = EntityState.Modified;

        foreach (var relation in entity.Relations)
        {
            context.Entry(relation).State = EntityState.Added;
        }
    }

    public void Remove(ExerciseCatalog entity) => context.ExerciseCatalogs.Remove(entity);

    public async Task<ExerciseCatalog?> GetByProviderExerciseIdAsync(
        string providerName, string providerExerciseId, CancellationToken cancellationToken = default) =>
        await context.ExerciseCatalogs
            .Include(e => e.AttributeContribution)
            .Include(e => e.Relations)
            .Include(e => e.Taxonomy)
            .FirstOrDefaultAsync(
                e => e.ProviderName == providerName &&
                     e.ProviderExerciseId == providerExerciseId,
                cancellationToken);

    /// US-239: mesma busca, sem o provider name (ver doc na interface).
    public async Task<ExerciseCatalog?> GetByProviderExerciseIdAsync(
        string providerExerciseId, CancellationToken cancellationToken = default) =>
        await context.ExerciseCatalogs
            .Include(e => e.Taxonomy)
            .FirstOrDefaultAsync(e => e.ProviderExerciseId == providerExerciseId, cancellationToken);

    /// US-206: cache-aside pattern — Redis TTL 30 min; any Redis failure falls through to DB.
    public async Task<IReadOnlyList<ExerciseCatalog>> ListApprovedForWorkoutGenerationAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cached = await catalogCache.GetApprovedCatalogAsync(cancellationToken);
            if (cached is not null)
                return cached;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable for exercise catalog cache; falling back to DB");
        }

        var result = await context.ExerciseCatalogs
            .AsNoTracking()
            .Include(e => e.AttributeContribution)
            .Include(e => e.Taxonomy)
            .Where(e => e.IsApprovedForWorkoutGeneration)
            .OrderBy(e => e.NamePtBr)
            .ToListAsync(cancellationToken);

        try
        {
            await catalogCache.SetApprovedCatalogAsync(result, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable when writing exercise catalog cache");
        }

        return result;
    }
}
