namespace Awaken.Contracts.Admin.Bugs;

public record AdminBugEventResponse(
    Guid Id,
    string EventType,
    string? OldValue,
    string? NewValue,
    string? Comment,
    Guid AdminId,
    DateTime CreatedAtUtc);
