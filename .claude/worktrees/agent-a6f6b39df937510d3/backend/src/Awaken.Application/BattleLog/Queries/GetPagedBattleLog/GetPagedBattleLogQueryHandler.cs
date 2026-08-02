using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.BattleLog;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.BattleLog.Queries.GetPagedBattleLog;

/// US-083: retorna logs paginados para assinantes. Acesso expirado e bloqueado
/// pelo ActiveAccessMiddleware (ADR-009). Backend e autoridade de ordenacao e dados.
public class GetPagedBattleLogQueryHandler(
    IQuestLogRepository questLogRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetPagedBattleLogQuery, PagedBattleLogResponse>
{
    public async Task<PagedBattleLogResponse> Handle(
        GetPagedBattleLogQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var (logs, hasMore) = await questLogRepository.GetPagedByUserIdAsync(
            userId, request.Page, request.PageSize, cancellationToken);

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

        var metadata = new BattleLogPageMetadata(request.Page, request.PageSize, hasMore);
        return new PagedBattleLogResponse(items, metadata);
    }
}
