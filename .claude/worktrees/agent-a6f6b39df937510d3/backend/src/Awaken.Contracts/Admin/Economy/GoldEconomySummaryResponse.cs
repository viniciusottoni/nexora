namespace Awaken.Contracts.Admin.Economy;

/// <summary>
/// US-229: resumo agregado da economia Gold para o painel admin.
/// RN-003: sem dados de pagamento/provider.
/// </summary>
public record GoldEconomySummaryResponse(
    long TotalGoldPurchased,
    long TotalGoldSpent,
    long TotalInCirculation,
    int OrdersGranted,
    int OrdersPending,
    int OrdersFailed,
    int OpenGoldAlerts,
    IReadOnlyList<GoldTopProductItem> TopProducts,
    IReadOnlyList<GoldAbnormalUserItem> AbnormalUsers,
    DateTime? LastReconciliationUtc,
    DateTime FromUtc,
    DateTime ToUtc);

public record GoldTopProductItem(string ProductKey, int OrderCount);

public record GoldAbnormalUserItem(Guid UserId, int AlertCount);
