using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.BattleLog;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.BattleLog.Queries.GetRecentBattleLog;

/// US-081: retorna os logs de quests concluidas do usuario autenticado,
/// ordenados do mais recente ao mais antigo. Backend e autoridade (ADR-009).
public class GetRecentBattleLogQueryHandler(
    IQuestLogRepository questLogRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetRecentBattleLogQuery, BattleLogResponse>
{
    public async Task<BattleLogResponse> Handle(
        GetRecentBattleLogQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var logs = await questLogRepository.GetRecentByUserIdAsync(
            userId, request.Limit, cancellationToken);

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

        return new BattleLogResponse(items);
    }
}
