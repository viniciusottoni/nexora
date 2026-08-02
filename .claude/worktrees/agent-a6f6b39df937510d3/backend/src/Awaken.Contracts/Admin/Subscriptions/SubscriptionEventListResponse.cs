namespace Awaken.Contracts.Admin.Subscriptions;

public record SubscriptionEventListResponse(
    IReadOnlyList<SubscriptionEventSummaryResponse> Items,
    int Total,
    int Page,
    int PageSize);
