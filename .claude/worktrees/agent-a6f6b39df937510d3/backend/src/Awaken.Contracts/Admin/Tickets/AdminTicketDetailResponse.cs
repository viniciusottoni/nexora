namespace Awaken.Contracts.Admin.Tickets;

public record AdminTicketDetailResponse(
    Guid Id,
    Guid UserId,
    string Category,
    string Status,
    string? Priority,
    Guid? AssignedAdminId,
    string Language,
    string Description,
    string? AppVersion,
    string? CorrelationId,
    DateTime CreatedAtUtc,
    IReadOnlyList<AdminTicketEventResponse> History);
