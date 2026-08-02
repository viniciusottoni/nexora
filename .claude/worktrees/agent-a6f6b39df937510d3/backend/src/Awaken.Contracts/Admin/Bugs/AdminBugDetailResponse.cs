namespace Awaken.Contracts.Admin.Bugs;

public record AdminBugDetailResponse(
    Guid Id,
    string Title,
    string Severity,
    string Status,
    string Component,
    string Environment,
    string Origin,
    string? Description,
    string? CorrelationId,
    Guid? RelatedTicketId,
    string? RelatedErrorId,
    Guid? AssignedAdminId,
    DateTime OccurredAtUtc,
    Guid CreatedByAdminId,
    DateTime CreatedAtUtc,
    IReadOnlyList<AdminBugEventResponse> History);
