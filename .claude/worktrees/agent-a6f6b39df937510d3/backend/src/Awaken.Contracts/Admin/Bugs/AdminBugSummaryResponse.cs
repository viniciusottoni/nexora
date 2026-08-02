namespace Awaken.Contracts.Admin.Bugs;

public record AdminBugSummaryResponse(
    Guid Id,
    string Title,
    string Severity,
    string Status,
    string Component,
    string Environment,
    string Origin,
    DateTime OccurredAtUtc,
    DateTime CreatedAtUtc);
