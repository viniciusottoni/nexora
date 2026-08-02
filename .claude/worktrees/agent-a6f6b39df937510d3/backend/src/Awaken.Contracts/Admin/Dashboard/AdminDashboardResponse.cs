namespace Awaken.Contracts.Admin.Dashboard;

/// <summary>
/// US-161 — dashboard operacional administrativo.
/// RN-003: nenhum dado pessoal é exposto nos cartões agregados.
/// RN-005: cada bloco é resolvido independentemente; falha em um não compromete os demais.
/// </summary>
public record AdminDashboardResponse(
    int TotalUsers,
    int Dau,
    int OpenTickets,
    decimal? Mrr,
    IReadOnlyList<DauPoint> DauTimeSeries,
    IReadOnlyList<ActivityFeedItem> RecentActivity,
    IReadOnlyList<TopEventItem> TopEvents);

public record DauPoint(DateOnly Date, int Count);

public record ActivityFeedItem(string Action, string ResourceType, DateTime CreatedAtUtc);

public record TopEventItem(string EventName, int Count);
