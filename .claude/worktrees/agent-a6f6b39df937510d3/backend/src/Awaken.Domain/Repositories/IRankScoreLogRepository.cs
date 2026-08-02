using Awaken.Domain.Common;
using Awaken.Domain.Entities.Progression;

namespace Awaken.Domain.Repositories;

/// US-154/US-155: persistência do log de auditoria de ganhos de RankScore.
public interface IRankScoreLogRepository : IRepository<RankScoreLog>
{
}
