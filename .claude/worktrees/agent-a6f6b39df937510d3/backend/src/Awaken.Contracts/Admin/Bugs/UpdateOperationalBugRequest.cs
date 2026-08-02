namespace Awaken.Contracts.Admin.Bugs;

public record UpdateOperationalBugRequest(
    string? Status,
    Guid? AssignedAdminId,
    string? Comment);
