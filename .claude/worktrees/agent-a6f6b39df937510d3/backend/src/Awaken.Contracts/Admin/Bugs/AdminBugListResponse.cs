namespace Awaken.Contracts.Admin.Bugs;

public record AdminBugListResponse(
    IReadOnlyList<AdminBugSummaryResponse> Items,
    int Total,
    int Page,
    int PageSize);
