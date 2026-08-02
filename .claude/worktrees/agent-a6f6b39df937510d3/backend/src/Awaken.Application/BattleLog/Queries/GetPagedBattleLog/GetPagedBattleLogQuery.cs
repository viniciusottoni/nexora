using Awaken.Contracts.BattleLog;
using MediatR;

namespace Awaken.Application.BattleLog.Queries.GetPagedBattleLog;

/// US-083: historico completo paginado para assinantes ativos.
public record GetPagedBattleLogQuery(int Page = 1, int PageSize = 20) : IRequest<PagedBattleLogResponse>;
