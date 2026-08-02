namespace Awaken.Contracts.Admin.Tickets;

public record AdminTicketEventResponse(
    Guid Id,
    string EventType,
    string? OldValue,
    string? NewValue,
    string? NoteContent,
    Guid AdminId,
    DateTime CreatedAtUtc);
