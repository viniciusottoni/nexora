namespace Awaken.Contracts.Admin.Tickets;

public record AdminTicketListResponse(
    IReadOnlyList<AdminTicketSummaryResponse> Items,
    int Total,
    int Page,
    int PageSize);
