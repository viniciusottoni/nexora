namespace Awaken.Contracts.Admin.Security;

public record SecurityAlertListResponse(
    IReadOnlyList<SecurityAlertSummaryResponse> Items,
    int Total,
    int Page,
    int PageSize);
