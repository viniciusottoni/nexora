using Awaken.Domain.Entities.Audit;

namespace Awaken.Application.Common.Interfaces;

public interface IAuditLogService
{
    Task RecordAsync(
        string action,
        Guid? actorUserId,
        AuditActorType actorType,
        string resourceType,
        Guid? resourceId,
        string? metadataSafe,
        CancellationToken cancellationToken = default);
}
