namespace Awaken.Contracts.Admin.Users;

public record AdminUserListResponse(
    IReadOnlyList<AdminUserSummaryResponse> Items,
    int Total,
    int Page,
    int PageSize);
