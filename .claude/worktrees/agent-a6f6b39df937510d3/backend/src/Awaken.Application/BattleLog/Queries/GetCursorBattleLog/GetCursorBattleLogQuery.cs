using Awaken.Contracts.BattleLog;
using MediatR;

namespace Awaken.Application.BattleLog.Queries.GetCursorBattleLog;

/// US-209: historico paginado por cursor para assinantes; mais estavel que offset sob insercoes concorrentes.
public record GetCursorBattleLogQuery(Guid? AfterCursor, int Limit = 20)
    : IRequest<CursorPagedResponse<BattleLogItemResponse>>;
