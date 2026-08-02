using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Timeline;
using Awaken.Domain.Repositories;

namespace Awaken.Infrastructure.Services;

public sealed class OperationalTimelineService : IOperationalTimelineService
{
    private const int MaxEntries = 100;
    private const int DefaultRangeHours = 24;

    private readonly ISecurityAlertRepository _securityAlertRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IOperationalBugRepository _operationalBugRepository;
    private readonly ISupportTicketRepository _supportTicketRepository;
    private readonly IDateTimeService _dateTimeService;

    public OperationalTimelineService(
        ISecurityAlertRepository securityAlertRepository,
        IAuditLogRepository auditLogRepository,
        IOperationalBugRepository operationalBugRepository,
        ISupportTicketRepository supportTicketRepository,
        IDateTimeService dateTimeService)
    {
        _securityAlertRepository = securityAlertRepository;
        _auditLogRepository = auditLogRepository;
        _operationalBugRepository = operationalBugRepository;
        _supportTicketRepository = supportTicketRepository;
        _dateTimeService = dateTimeService;
    }

    public async Task<OperationalTimelineResponse> GetTimelineAsync(
        OperationalTimelineFilters filters,
        CancellationToken ct)
    {
        var now = _dateTimeService.UtcNow;
        var from = filters.From ?? now.AddHours(-DefaultRangeHours);
        var to = filters.To ?? now;

        var entries = new List<TimelineEntryResponse>();

        // Security alerts
        var securityPage = await _securityAlertRepository.GetPagedAsync(
            alertType: null,
            severity: filters.Severity,
            status: null,
            environment: filters.Environment,
            page: 1,
            pageSize: MaxEntries,
            ct);

        foreach (var alert in securityPage.Items)
        {
            if (alert.CreatedAtUtc < from || alert.CreatedAtUtc > to) continue;
            if (filters.UserId.HasValue && alert.AffectedUserId != filters.UserId) continue;

            entries.Add(new TimelineEntryResponse(
                Id: alert.Id.ToString(),
                EntryType: "security_alert",
                Title: $"Alerta de segurança: {alert.AlertType}",
                Description: $"Severidade: {alert.Severity}. Status: {alert.Status}. Ambiente: {alert.Environment}.",
                Severity: alert.Severity,
                OccurredAtUtc: alert.CreatedAtUtc,
                MaskedUserId: MaskGuid(alert.AffectedUserId),
                ResourceId: null,
                ResourceType: "security_alert",
                CorrelationId: null,
                IsRelationCertain: true,
                DetailUrl: $"/admin/security"));
        }

        // Audit logs
        var auditPage = await _auditLogRepository.GetPagedAsync(
            actorType: null,
            action: null,
            resourceType: filters.Resource,
            from: from,
            to: to,
            page: 1,
            pageSize: MaxEntries,
            ct);

        foreach (var log in auditPage.Items)
        {
            if (filters.UserId.HasValue && log.ActorUserId != filters.UserId) continue;
            if (filters.Severity != null) continue; // audit logs have no severity — skip when severity filter is active

            entries.Add(new TimelineEntryResponse(
                Id: log.Id.ToString(),
                EntryType: "audit_log",
                Title: $"Ação auditada: {log.Action}",
                Description: $"Tipo de ator: {log.ActorType}. Recurso: {log.ResourceType}{(log.ResourceId.HasValue ? $" ({log.ResourceId})" : "")}.",
                Severity: "info",
                OccurredAtUtc: log.CreatedAtUtc,
                MaskedUserId: MaskGuid(log.ActorUserId),
                ResourceId: log.ResourceId?.ToString(),
                ResourceType: log.ResourceType,
                CorrelationId: log.CorrelationId,
                IsRelationCertain: true,
                DetailUrl: $"/admin/audit"));
        }

        // Operational bugs
        var bugPage = await _operationalBugRepository.GetPagedAsync(
            severity: filters.Severity,
            status: null,
            component: null,
            environment: filters.Environment,
            origin: null,
            page: 1,
            pageSize: MaxEntries,
            ct);

        foreach (var bug in bugPage.Items)
        {
            if (bug.OccurredAtUtc < from || bug.OccurredAtUtc > to) continue;
            if (filters.Resource != null && !bug.Component.Contains(filters.Resource, StringComparison.OrdinalIgnoreCase)) continue;

            var isRelationCertain = bug.CorrelationId != null;

            entries.Add(new TimelineEntryResponse(
                Id: bug.Id.ToString(),
                EntryType: "bug",
                Title: bug.Title,
                Description: bug.Description ?? $"Componente: {bug.Component}. Ambiente: {bug.Environment}.",
                Severity: bug.Severity,
                OccurredAtUtc: bug.OccurredAtUtc,
                MaskedUserId: null,
                ResourceId: bug.Id.ToString(),
                ResourceType: "bug",
                CorrelationId: bug.CorrelationId,
                IsRelationCertain: isRelationCertain,
                DetailUrl: $"/admin/bugs/{bug.Id}"));
        }

        // Support tickets
        var ticketPage = await _supportTicketRepository.GetPagedAsync(
            status: null,
            priority: filters.Severity == "critical" ? "critical" : null,
            category: null,
            page: 1,
            pageSize: MaxEntries,
            ct);

        foreach (var ticket in ticketPage.Items)
        {
            if (ticket.CreatedAtUtc < from || ticket.CreatedAtUtc > to) continue;
            if (filters.UserId.HasValue && ticket.UserId != filters.UserId) continue;
            if (filters.Severity != null && ticket.Priority != filters.Severity) continue;

            var isRelationCertain = ticket.CorrelationId != null;

            entries.Add(new TimelineEntryResponse(
                Id: ticket.Id.ToString(),
                EntryType: "ticket",
                Title: $"Ticket de suporte: {ticket.Category}",
                Description: ticket.Description.Length > 200
                    ? ticket.Description[..200] + "..."
                    : ticket.Description,
                Severity: ticket.Priority ?? "info",
                OccurredAtUtc: ticket.CreatedAtUtc,
                MaskedUserId: MaskGuid(ticket.UserId),
                ResourceId: null,
                ResourceType: "ticket",
                CorrelationId: ticket.CorrelationId,
                IsRelationCertain: isRelationCertain,
                DetailUrl: $"/admin/tickets/{ticket.Id}"));
        }

        // Sort descending by date, take max 100
        var sorted = entries
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(MaxEntries)
            .ToList();

        var impact = BuildImpactSummary(sorted, from, to);

        return new OperationalTimelineResponse(
            Entries: sorted,
            Impact: impact,
            GeneratedAtUtc: now);
    }

    private static ImpactSummaryResponse BuildImpactSummary(
        IReadOnlyList<TimelineEntryResponse> entries,
        DateTime from,
        DateTime to)
    {
        if (entries.Count == 0)
        {
            return new ImpactSummaryResponse(0, 0, null, null, null);
        }

        var usersAffected = entries
            .Where(e => e.MaskedUserId != null)
            .Select(e => e.MaskedUserId!)
            .Distinct()
            .Count();

        var resourcesAffected = entries
            .Where(e => e.ResourceId != null)
            .Select(e => e.ResourceId!)
            .Distinct()
            .Count();

        var severityOrder = new[] { "critical", "high", "attention", "medium", "low", "info" };
        var peakSeverity = severityOrder.FirstOrDefault(s =>
            entries.Any(e => string.Equals(e.Severity, s, StringComparison.OrdinalIgnoreCase)));

        return new ImpactSummaryResponse(
            EstimatedUsersAffected: usersAffected,
            ResourcesAffected: resourcesAffected,
            PeakSeverity: peakSeverity,
            PeriodStart: from,
            PeriodEnd: to);
    }

    private static string? MaskGuid(Guid? id)
    {
        if (id is null || id == Guid.Empty) return null;
        return id.Value.ToString("N")[..8]; // first 8 chars of compact GUID
    }

    private static string MaskGuid(Guid id)
    {
        return id.ToString("N")[..8];
    }
}
