using Awaken.Contracts.BattleLog;
using MediatR;

namespace Awaken.Application.BattleLog.Queries.GetRecentBattleLog;

public record GetRecentBattleLogQuery(int Limit = 20) : IRequest<BattleLogResponse>;
