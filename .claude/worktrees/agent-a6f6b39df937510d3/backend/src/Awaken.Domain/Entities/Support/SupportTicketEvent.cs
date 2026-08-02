using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Support;

public class SupportTicketEvent : BaseEntity
{
    public Guid TicketId { get; private set; }
    public string EventType { get; private set; } = string.Empty; // "status_change"|"priority_change"|"assigned"|"internal_note"
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? NoteContent { get; private set; }
    public Guid AdminId { get; private set; }

    private SupportTicketEvent() { }

    public static SupportTicketEvent Create(Guid ticketId, string eventType, string? oldValue, string? newValue, string? noteContent, Guid adminId, DateTime utcNow) =>
        new() { TicketId = ticketId, EventType = eventType, OldValue = oldValue, NewValue = newValue, NoteContent = noteContent, AdminId = adminId, CreatedAtUtc = utcNow };
}
