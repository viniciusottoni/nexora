namespace Awaken.Contracts.Admin.Audit;

public record AuditLogListResponse(
    IReadOnlyList<AuditLogSummaryResponse> Items,
    int Total,
    int Page,
    int PageSize);
