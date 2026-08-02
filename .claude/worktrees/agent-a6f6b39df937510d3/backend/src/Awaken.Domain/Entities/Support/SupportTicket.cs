using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Support;

public class SupportTicket : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Category { get; private set; } = string.Empty; // "report" | "question" | "suggestion"
    public string Status { get; private set; } = "open";
    public string Language { get; private set; } = "pt-BR";
    public string Description { get; private set; } = string.Empty;
    public string? AppVersion { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? Priority { get; private set; } // "low" | "medium" | "high" | "critical"
    public Guid? AssignedAdminId { get; private set; }

    private SupportTicket() { }

    public static SupportTicket Create(
        Guid userId,
        string category,
        string language,
        string description,
        string? appVersion,
        string? correlationId,
        DateTime utcNow)
    {
        return new SupportTicket
        {
            UserId = userId,
            Category = category.ToLowerInvariant(),
            Language = language,
            Description = description,
            AppVersion = appVersion,
            CorrelationId = correlationId,
            Status = "open",
            CreatedAtUtc = utcNow,
        };
    }

    public void UpdateTriage(string? priority, Guid? assignedAdminId, DateTime utcNow)
    {
        Priority = priority;
        AssignedAdminId = assignedAdminId;
        UpdatedAtUtc = utcNow;
    }

    public void UpdateStatus(string status, DateTime utcNow)
    {
        Status = status;
        UpdatedAtUtc = utcNow;
    }
}
