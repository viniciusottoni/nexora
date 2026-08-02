using Awaken.Domain.Entities.Training;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

/// US-231: tabela pequena e estatica (7 linhas) — sem cache Redis, EF direto.
public class TrainingProgramRepository(AwakenDbContext context) : ITrainingProgramRepository
{
    public async Task<IReadOnlyList<TrainingProgram>> GetActiveProgramsAsync(CancellationToken ct = default) =>
        (await context.TrainingPrograms
            .AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync(ct))
        .OrderBy(p => GetCatalogOrder(p.Key))
        .ThenBy(p => p.Key)
        .ToList();

    public async Task<TrainingProgram?> GetByKeyAsync(string key, CancellationToken ct = default) =>
        await context.TrainingPrograms.FirstOrDefaultAsync(p => p.Key == key && p.IsActive, ct);

    private static int GetCatalogOrder(string key)
    {
        var index = Array.IndexOf(TrainingProgramKeys.CatalogOrder, key);
        return index >= 0 ? index : int.MaxValue;
    }
}
