using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class ExerciseRawImportRepository(AwakenDbContext context) : IExerciseRawImportRepository
{
    public async Task<ExerciseRawImport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.ExerciseRawImports.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IEnumerable<ExerciseRawImport>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.ExerciseRawImports.ToListAsync(cancellationToken);

    public async Task AddAsync(ExerciseRawImport entity, CancellationToken cancellationToken = default) =>
        await context.ExerciseRawImports.AddAsync(entity, cancellationToken);

    public void Update(ExerciseRawImport entity) => context.ExerciseRawImports.Update(entity);

    public void Remove(ExerciseRawImport entity) => context.ExerciseRawImports.Remove(entity);

    public async Task<bool> ExistsByProviderExerciseIdAsync(
        string providerName, string providerExerciseId, CancellationToken cancellationToken = default) =>
        await context.ExerciseRawImports.AnyAsync(
            e => e.ProviderName == providerName &&
                 e.ProviderExerciseId == providerExerciseId,
            cancellationToken);

    public async Task<ExerciseRawImport?> GetByProviderExerciseIdAsync(
        string providerName, string providerExerciseId, CancellationToken cancellationToken = default) =>
        await context.ExerciseRawImports.FirstOrDefaultAsync(
            e => e.ProviderName == providerName &&
                 e.ProviderExerciseId == providerExerciseId,
            cancellationToken);
}
