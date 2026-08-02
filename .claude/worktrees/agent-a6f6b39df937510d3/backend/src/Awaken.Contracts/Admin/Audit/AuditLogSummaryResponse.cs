namespace Awaken.Contracts.Admin.Audit;

public record AuditLogSummaryResponse(
    Guid Id,
    Guid? ActorUserId,
    string ActorType,
    string Action,
    string ResourceType,
    Guid? ResourceId,
    DateTime CreatedAtUtc,
    string? CorrelationId);
