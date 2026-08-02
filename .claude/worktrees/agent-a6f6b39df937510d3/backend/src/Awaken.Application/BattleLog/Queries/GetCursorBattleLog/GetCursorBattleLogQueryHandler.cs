using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.BattleLog;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.BattleLog.Queries.GetCursorBattleLog;

/// US-209: retorna logs paginados por cursor para assinantes. Acesso expirado e bloqueado
/// pelo ActiveAccessMiddleware (ADR-009). Backend e autoridade de ordenacao e dados.
public class GetCursorBattleLogQueryHandler(
    IQuestLogRepository questLogRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetCursorBattleLogQuery, CursorPagedResponse<BattleLogItemResponse>>
{
    public async Task<CursorPagedResponse<BattleLogItemResponse>> Handle(
        GetCursorBattleLogQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var (logs, hasMore) = await questLogRepository.GetPageByUserIdAsync(
            userId, request.AfterCursor, request.Limit, cancellationToken);

        var items = logs
            .Select(l => new BattleLogItemResponse(
                l.Id,
                l.QuestId,
                l.QuestType,
                l.XpEarned,
                l.XpPenaltyApplied,
                l.ItemsEarned,
                l.CompletedAtUtc))
            .ToList();

        var nextCursor = hasMore && logs.Count > 0
            ? logs[^1].Id.ToString()
            : null;

        return new CursorPagedResponse<BattleLogItemResponse>(items, nextCursor, hasMore);
    }
}
