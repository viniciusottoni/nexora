using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Bugs;

public class OperationalBugEvent : BaseEntity
{
    public Guid BugId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? Comment { get; private set; }
    public Guid AdminId { get; private set; }

    private OperationalBugEvent() { }

    public static OperationalBugEvent Create(Guid bugId, string eventType, string? oldValue, string? newValue, string? comment, Guid adminId, DateTime utcNow) =>
        new() { BugId = bugId, EventType = eventType, OldValue = oldValue, NewValue = newValue, Comment = comment, AdminId = adminId, CreatedAtUtc = utcNow };
}
