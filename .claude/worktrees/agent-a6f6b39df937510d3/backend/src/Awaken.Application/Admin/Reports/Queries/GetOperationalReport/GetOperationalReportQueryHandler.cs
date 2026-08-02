using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Reports;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Reports.Queries.GetOperationalReport;

/// <summary>
/// US-170: handler de relatório operacional consolidado.
/// Agrega dados de usuários, tickets, bugs, alertas e analytics no período solicitado.
/// Período padrão: últimos 7 dias quando From/To não informados.
/// </summary>
public class GetOperationalReportQueryHandler(
    IAdminAnalyticsRepository analyticsRepo,
    ISupportTicketRepository ticketRepo,
    IOperationalBugRepository bugRepo,
    ISecurityAlertRepository alertRepo,
    IDateTimeService dateTimeService)
    : IRequestHandler<GetOperationalReportQuery, OperationalReportResponse>
{
    public async Task<OperationalReportResponse> Handle(GetOperationalReportQuery request, CancellationToken ct)
    {
        var utcNow = dateTimeService.UtcNow;
        var from = request.From ?? utcNow.Date.AddDays(-7);
        var to = request.To ?? utcNow;
        var environment = request.Environment ?? "all";

        // ── Users / DAU ──────────────────────────────────────────────────────
        var totalUsers = await analyticsRepo.CountUsersAsync(ct);
        var dau = await analyticsRepo.CountDistinctActiveUsersSinceAsync(utcNow.Date, ct);
        var mau = await analyticsRepo.CountDistinctActiveUsersSinceAsync(utcNow.AddDays(-30), ct);
        double? dauMauRatio = mau > 0 ? (double)dau / mau : null;

        // ── Tickets ──────────────────────────────────────────────────────────
        var (allTickets, totalTickets) = await ticketRepo.GetPagedAsync(null, null, null, 1, 10_000, ct);
        var openTickets = allTickets.Count(t => t.Status == "open");
        var inProgressTickets = allTickets.Count(t => t.Status == "in_progress");
        var resolvedTickets = allTickets.Count(t => t.Status == "resolved");
        var closedTickets = allTickets.Count(t => t.Status == "closed");
        var highPriorityTickets = allTickets.Count(t => t.Priority is "high" or "critical");
        var newTickets = allTickets.Count(t => t.CreatedAtUtc >= from && t.CreatedAtUtc <= to);
        var openAlerts = await analyticsRepo.CountOpenSupportTicketsAsync(ct);

        // ── Bugs ─────────────────────────────────────────────────────────────
        var envFilter = environment == "all" ? null : environment;
        var (allBugs, totalBugs) = await bugRepo.GetPagedAsync(null, null, null, envFilter, null, 1, 10_000, ct);
        var openBugs = allBugs.Count(b => b.Status is "open" or "in_progress");
        var criticalBugs = allBugs.Count(b => b.Severity == "critical");
        var highBugs = allBugs.Count(b => b.Severity == "high");
        var resolvedBugsThisPeriod = allBugs.Count(b =>
            b.Status is "resolved" or "closed" &&
            b.UpdatedAtUtc >= from && b.UpdatedAtUtc <= to);
        var newBugsThisPeriod = allBugs.Count(b => b.CreatedAtUtc >= from && b.CreatedAtUtc <= to);

        // ── Security alerts ──────────────────────────────────────────────────
        var (allAlerts, _) = await alertRepo.GetPagedAsync(null, null, null, envFilter, 1, 10_000, ct);
        var openSecurityAlerts = allAlerts.Count(a => a.Status == "open");

        // ── Product / Top events ─────────────────────────────────────────────
        var topEvents = await analyticsRepo.GetTopEventsAsync(from, to, 1, ct);
        var topEvent = topEvents.FirstOrDefault();

        var dailyOps = new DailyOperationsReport(
            TotalUsers: totalUsers,
            Dau: dau,
            OpenTickets: openTickets,
            NewTickets: newTickets,
            OpenBugs: openBugs,
            NewBugs: newBugsThisPeriod,
            OpenAlerts: openSecurityAlerts);

        var support = new SupportReport(
            Total: totalTickets,
            Open: openTickets,
            InProgress: inProgressTickets,
            Resolved: resolvedTickets,
            Closed: closedTickets,
            HighPriority: highPriorityTickets);

        var technical = new TechnicalReport(
            TotalBugs: totalBugs,
            CriticalBugs: criticalBugs,
            HighBugs: highBugs,
            ResolvedThisPeriod: resolvedBugsThisPeriod);

        var product = new ProductReport(
            TopEventCount: topEvent == default ? null : topEvent.Count,
            TopEventName: topEvent == default ? null : topEvent.Action,
            Dau: dau,
            Mau: mau,
            DauMauRatio: dauMauRatio);

        return new OperationalReportResponse(
            From: DateOnly.FromDateTime(from),
            To: DateOnly.FromDateTime(to),
            Environment: environment,
            DailyOps: dailyOps,
            Support: support,
            Technical: technical,
            Product: product);
    }
}
