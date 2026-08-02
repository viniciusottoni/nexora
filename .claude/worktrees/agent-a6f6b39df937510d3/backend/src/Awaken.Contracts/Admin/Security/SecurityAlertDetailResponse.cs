namespace Awaken.Contracts.Admin.Security;

public record SecurityAlertDetailResponse(
    Guid Id,
    string AlertType,
    string Severity,
    string Status,
    string? Origin,
    string? MaskedIp,
    Guid? AffectedUserId,
    string Environment,
    Guid? AnalyzedByAdminId,
    DateTime? AnalyzedAtUtc,
    DateTime CreatedAtUtc,
    string? Classification,
    string? Note,
    Guid? RelatedBugId);
