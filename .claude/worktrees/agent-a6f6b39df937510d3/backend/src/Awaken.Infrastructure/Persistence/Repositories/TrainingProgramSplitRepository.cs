using Awaken.Domain.Entities.Training;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

/// US-237: split map por programa — tabela pequena e estática, sem cache
/// Redis (mesmo padrão de <see cref="TrainingProgramRepository"/>).
public class TrainingProgramSplitRepository(AwakenDbContext context) : ITrainingProgramSplitRepository
{
    public async Task<TrainingProgramSplit?> GetByProgramKeyAsync(string programKey, CancellationToken ct = default) =>
        await context.TrainingProgramSplits
            .AsNoTracking()
            .Include(s => s.Days.OrderBy(d => d.DayIndex))
            .Where(s => s.ProgramKey == programKey && s.IsActive)
            .FirstOrDefaultAsync(ct);
}
