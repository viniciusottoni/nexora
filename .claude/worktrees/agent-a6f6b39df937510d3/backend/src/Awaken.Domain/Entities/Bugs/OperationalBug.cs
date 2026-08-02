using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Bugs;

public class OperationalBug : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Severity { get; private set; } = string.Empty; // "low"|"medium"|"high"|"critical"
    public string Status { get; private set; } = "open"; // "open"|"in_progress"|"resolved"|"closed"
    public string Component { get; private set; } = string.Empty;
    public string Environment { get; private set; } = string.Empty; // "dev"|"staging"|"prod"
    public string Origin { get; private set; } = string.Empty; // "user_report"|"monitoring"|"internal"
    public string? Description { get; private set; }
    public string? CorrelationId { get; private set; }
    public Guid? RelatedTicketId { get; private set; }
    public string? RelatedErrorId { get; private set; }
    public Guid? AssignedAdminId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public Guid CreatedByAdminId { get; private set; }

    private OperationalBug() { }

    public static OperationalBug Create(string title, string severity, string component, string environment, string origin, Guid createdByAdminId, DateTime occurredAtUtc, DateTime utcNow, string? description = null, string? correlationId = null, Guid? relatedTicketId = null, string? relatedErrorId = null) =>
        new() { Title = title, Severity = severity, Component = component, Environment = environment, Origin = origin, CreatedByAdminId = createdByAdminId, OccurredAtUtc = occurredAtUtc, CreatedAtUtc = utcNow, Description = description, CorrelationId = correlationId, RelatedTicketId = relatedTicketId, RelatedErrorId = relatedErrorId };

    public void UpdateStatus(string newStatus, DateTime utcNow) { Status = newStatus; UpdatedAtUtc = utcNow; }
    public void Assign(Guid adminId, DateTime utcNow) { AssignedAdminId = adminId; UpdatedAtUtc = utcNow; }
    public void Update(string title, string severity, string? description, string? correlationId, DateTime utcNow) { Title = title; Severity = severity; Description = description; CorrelationId = correlationId; UpdatedAtUtc = utcNow; }
}
