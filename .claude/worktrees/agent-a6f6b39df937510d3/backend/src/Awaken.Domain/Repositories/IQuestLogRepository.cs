using Awaken.Domain.Common;
using Awaken.Domain.Entities.Quests;

namespace Awaken.Domain.Repositories;

public interface IQuestLogRepository : IRepository<QuestLog>
{
    /// US-062 RN-007: usado para garantir que cada quest gere no maximo um QuestLog.
    Task<QuestLog?> GetByQuestIdAsync(Guid questId, CancellationToken cancellationToken = default);

    /// US-081 RN-001/RN-004: logs recentes do usuario, ordenados do mais novo ao mais antigo.
    Task<IReadOnlyList<QuestLog>> GetRecentByUserIdAsync(Guid userId, int limit, CancellationToken cancellationToken = default);

    /// US-083 RN-005: logs paginados para assinantes; retorna hasMore para scroll incremental.
    Task<(IReadOnlyList<QuestLog> Items, bool HasMore)> GetPagedByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// US-209: cursor-based pagination — mais estavel que offset sob insercoes concorrentes.
    Task<(List<QuestLog> Items, bool HasMore)> GetPageByUserIdAsync(
        Guid userId,
        Guid? afterId,
        int limit,
        CancellationToken cancellationToken = default);

    /// US-241 §9.1: logs concluídos desde uma data (para extrair o sentimento
    /// predominante da semana). Ordenado do mais recente ao mais antigo.
    Task<IReadOnlyList<QuestLog>> GetCompletedSinceAsync(
        Guid userId, DateTime sinceUtc, CancellationToken cancellationToken = default);
}
