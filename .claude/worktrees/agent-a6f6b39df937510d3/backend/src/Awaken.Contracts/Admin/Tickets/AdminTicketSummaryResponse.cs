namespace Awaken.Contracts.Admin.Tickets;

public record AdminTicketSummaryResponse(
    Guid Id,
    Guid UserId,
    string Category,
    string Status,
    string? Priority,
    Guid? AssignedAdminId,
    string Description,
    DateTime CreatedAtUtc);
