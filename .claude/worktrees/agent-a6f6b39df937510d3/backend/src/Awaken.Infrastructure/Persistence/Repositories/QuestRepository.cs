using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class QuestRepository(AwakenDbContext context) : IQuestRepository
{
    public async Task<Quest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Quests.FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

    public async Task<IEnumerable<Quest>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Quests.ToListAsync(cancellationToken);

    public async Task AddAsync(Quest entity, CancellationToken cancellationToken = default) =>
        await context.Quests.AddAsync(entity, cancellationToken);

    public void Update(Quest entity)
    {
        context.Quests.Update(entity);
    }

    public void UpdateRoot(Quest entity) =>
        context.Entry(entity).State = EntityState.Modified;

    public async Task AddExercisesAsync(IEnumerable<QuestExercise> exercises, CancellationToken cancellationToken = default) =>
        await context.QuestExercises.AddRangeAsync(exercises, cancellationToken);

    public void Remove(Quest entity) => context.Quests.Remove(entity);

    public async Task<Quest?> GetByUserIdAndDateAsync(
        Guid userId,
        string type,
        DateTime questDateUtc,
        CancellationToken cancellationToken = default) =>
        await context.Quests
            .AsNoTracking()
            .Include(q => q.Exercises)
            .FirstOrDefaultAsync(
                q => q.UserId == userId && q.Type == type && q.QuestDateUtc == questDateUtc,
                cancellationToken);

    public async Task<List<Quest>> GetUncheckedDailiesByDateAsync(
        DateTime questDateUtc,
        CancellationToken cancellationToken = default) =>
        await context.Quests
            .Where(q => q.Type == "daily" && q.QuestDateUtc == questDateUtc && q.PenaltyCheckedAtUtc == null)
            .ToListAsync(cancellationToken);

    /// US-207: versao paginada por cursor de GetUncheckedDailiesByDateAsync.
    public async Task<List<Quest>> GetUncheckedDailiesPageAsync(
        DateTime questDate,
        Guid? afterId,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Quest> query = context.Quests
            .Where(q => q.Type == "daily"
                && q.QuestDateUtc == questDate
                && q.PenaltyCheckedAtUtc == null)
            .OrderBy(q => q.Id);

        if (afterId.HasValue)
            query = query.Where(q => q.Id > afterId.Value);

        return await query.Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task<List<Quest>> GetUncheckedDailiesBeforeDateForUserAsync(
        Guid userId,
        DateTime beforeQuestDateUtc,
        CancellationToken cancellationToken = default) =>
        await context.Quests
            .Where(q => q.UserId == userId
                && q.Type == "daily"
                && q.QuestDateUtc < beforeQuestDateUtc
                && q.PenaltyCheckedAtUtc == null)
            .OrderBy(q => q.QuestDateUtc)
            .ThenBy(q => q.Id)
            .ToListAsync(cancellationToken);

    public async Task<List<Quest>> GetDailiesForUserBetweenDatesAsync(
        Guid userId,
        DateTime fromQuestDateUtc,
        DateTime toQuestDateUtc,
        CancellationToken cancellationToken = default) =>
        await context.Quests
            .AsNoTracking()
            .Where(q => q.UserId == userId
                && q.Type == "daily"
                && q.QuestDateUtc >= fromQuestDateUtc
                && q.QuestDateUtc <= toQuestDateUtc)
            .OrderBy(q => q.QuestDateUtc)
            .ThenBy(q => q.Id)
            .ToListAsync(cancellationToken);

    public async Task<List<Quest>> GetMissedPenaltyCheckedByDateAsync(
        DateTime questDateUtc,
        CancellationToken cancellationToken = default) =>
        await context.Quests
            .AsNoTracking()
            .Where(q => q.Type == "daily"
                && q.QuestDateUtc == questDateUtc
                && q.PenaltyCheckedAtUtc != null
                && q.Status != "completed")
            .ToListAsync(cancellationToken);

    public async Task<Quest?> GetByIdWithExercisesAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Quests
            .Include(q => q.Exercises)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

    /// US-238: último Quest concluído do usuário no mesmo programa (RN-001/RN-004),
    /// base para a rotação cíclica do dia.
    public async Task<Quest?> GetLastCompletedByUserAndProgramAsync(
        Guid userId, string programKey, CancellationToken cancellationToken = default) =>
        await context.Quests
            .AsNoTracking()
            .Where(q => q.UserId == userId
                && ((q.TrainingType == "program" && q.ProgramId == programKey)
                    || q.ResolvedProgramKey == programKey)
                && q.Status == "completed")
            .OrderByDescending(q => q.CompletedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
}
