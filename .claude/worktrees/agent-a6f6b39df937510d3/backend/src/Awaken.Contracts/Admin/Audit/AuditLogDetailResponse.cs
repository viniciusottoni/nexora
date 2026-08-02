namespace Awaken.Contracts.Admin.Audit;

public record AuditLogDetailResponse(
    Guid Id,
    Guid? ActorUserId,
    string ActorType,
    string Action,
    string ResourceType,
    Guid? ResourceId,
    string? MetadataSafe,
    string? CorrelationId,
    DateTime CreatedAtUtc);
