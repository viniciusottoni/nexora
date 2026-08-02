using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Audit;

public enum AuditActorType { User, System, Admin }

public class AuditLog : BaseEntity
{
    public Guid? ActorUserId { get; private set; }
    public AuditActorType ActorType { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string ResourceType { get; private set; } = string.Empty;
    public Guid? ResourceId { get; private set; }
    public string? MetadataSafe { get; private set; }
    public string? CorrelationId { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string action,
        Guid? actorUserId,
        AuditActorType actorType,
        string resourceType,
        Guid? resourceId,
        string? metadataSafe,
        string? correlationId,
        DateTime utcNow) =>
        new()
        {
            Action = action,
            ActorUserId = actorUserId,
            ActorType = actorType,
            ResourceType = resourceType,
            ResourceId = resourceId,
            MetadataSafe = metadataSafe,
            CorrelationId = correlationId,
            CreatedAtUtc = utcNow,
        };
}
