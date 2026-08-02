namespace Awaken.Contracts.Admin.Economy;

public record GoldOrderPageResponse(
    IReadOnlyList<GoldOrderAdminResponse> Items,
    int Total,
    int Page,
    int PageSize);
