namespace Awaken.Contracts.Admin.Economy;

public record GoldLedgerPageResponse(
    IReadOnlyList<GoldLedgerEntryAdminResponse> Items,
    int Total,
    int Page,
    int PageSize);
