namespace Awaken.Contracts.Admin.Bugs;

public record CreateOperationalBugRequest(
    string Title,
    string Severity,
    string Component,
    string Environment,
    string Origin,
    DateTime OccurredAtUtc,
    string? Description,
    string? CorrelationId,
    Guid? RelatedTicketId,
    string? RelatedErrorId);
