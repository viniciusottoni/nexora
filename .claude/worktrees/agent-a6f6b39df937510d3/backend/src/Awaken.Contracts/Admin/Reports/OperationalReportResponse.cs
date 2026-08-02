namespace Awaken.Contracts.Admin.Reports;

public record OperationalReportResponse(
    DateOnly From,
    DateOnly To,
    string Environment,
    DailyOperationsReport DailyOps,
    SupportReport Support,
    TechnicalReport Technical,
    ProductReport Product);

public record DailyOperationsReport(
    int TotalUsers,
    int Dau,
    int OpenTickets,
    int NewTickets,
    int OpenBugs,
    int NewBugs,
    int OpenAlerts);

public record SupportReport(
    int Total,
    int Open,
    int InProgress,
    int Resolved,
    int Closed,
    int HighPriority);

public record TechnicalReport(
    int TotalBugs,
    int CriticalBugs,
    int HighBugs,
    int ResolvedThisPeriod);

public record ProductReport(
    int? TopEventCount,
    string? TopEventName,
    int? Dau,
    int? Mau,
    double? DauMauRatio);
