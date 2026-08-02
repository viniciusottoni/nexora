using Awaken.Domain.Common;
using Awaken.Domain.Entities.Quests;

namespace Awaken.Domain.Repositories;

public interface IQuestRepository : IRepository<Quest>
{
    Task<Quest?> GetByUserIdAndDateAsync(
        Guid userId,
        string type,
        DateTime questDateUtc,
        CancellationToken cancellationToken = default);

    /// US-129: dailies da data informada ainda nao verificadas pela virada de dia, completadas ou nao.
    Task<List<Quest>> GetUncheckedDailiesByDateAsync(
        DateTime questDateUtc,
        CancellationToken cancellationToken = default);

    /// US-207: versao paginada por cursor de GetUncheckedDailiesByDateAsync.
    Task<List<Quest>> GetUncheckedDailiesPageAsync(
        DateTime questDate,
        Guid? afterId,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// US-070/US-129: dailies anteriores ao "hoje" local do usuario ainda nao verificadas.
    Task<List<Quest>> GetUncheckedDailiesBeforeDateForUserAsync(
        Guid userId,
        DateTime beforeQuestDateUtc,
        CancellationToken cancellationToken = default);

    Task<List<Quest>> GetDailiesForUserBetweenDatesAsync(
        Guid userId,
        DateTime fromQuestDateUtc,
        DateTime toQuestDateUtc,
        CancellationToken cancellationToken = default);

    /// US-135: dailies do dia anterior com penalidade verificada e status diferente de completed.
    Task<List<Quest>> GetMissedPenaltyCheckedByDateAsync(
        DateTime questDateUtc,
        CancellationToken cancellationToken = default);

    /// US-057/US-058: carrega a quest com os QuestExercise materializados (sem ordenacao garantida pelo Include).
    Task<Quest?> GetByIdWithExercisesAsync(Guid id, CancellationToken cancellationToken = default);

    void UpdateRoot(Quest entity);

    Task AddExercisesAsync(IEnumerable<QuestExercise> exercises, CancellationToken cancellationToken = default);

    /// US-238: último Quest concluído do usuário no mesmo programa, para a rotação cíclica.
    Task<Quest?> GetLastCompletedByUserAndProgramAsync(
        Guid userId, string programKey, CancellationToken cancellationToken = default);
}
