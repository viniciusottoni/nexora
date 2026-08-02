namespace Awaken.Contracts.Admin.Security;

public record SecurityAlertSummaryResponse(
    Guid Id,
    string AlertType,
    string Severity,
    string Status,
    string? Origin,
    string Environment,
    DateTime CreatedAtUtc,
    string? Classification);
