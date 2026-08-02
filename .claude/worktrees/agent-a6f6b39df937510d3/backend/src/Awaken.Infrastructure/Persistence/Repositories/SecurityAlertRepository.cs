using Awaken.Domain.Entities.Security;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class SecurityAlertRepository(AwakenDbContext context) : ISecurityAlertRepository
{
    /// <summary>
    /// US-219 RN-002: alertas críticos devem aparecer antes de alertas baixos.
    /// Mapeamento estável usado apenas para ordenação (não persistido).
    /// </summary>
    private static int SeverityRank(string severity) => severity switch
    {
        "critical" => 0,
        "high" => 1,
        "medium" => 2,
        "low" => 3,
        _ => 4,
    };

    public async Task AddAsync(SecurityAlert alert, CancellationToken ct = default) =>
        await context.SecurityAlerts.AddAsync(alert, ct);

    public async Task<SecurityAlert?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.SecurityAlerts.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct);

    public async Task<(IReadOnlyList<SecurityAlert> Items, int Total)> GetPagedAsync(
        string? alertType, string? severity, string? status, string? environment,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.SecurityAlerts.Where(a => !a.IsDeleted);

        if (!string.IsNullOrWhiteSpace(alertType))
            query = query.Where(a => a.AlertType == alertType);

        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(a => a.Severity == severity);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.Status == status);

        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(a => a.Environment == environment);

        var total = await query.CountAsync(ct);

        // RN-002: critical > high > medium > low, depois mais recente primeiro.
        // EF Core não traduz o switch acima para SQL, então ordenamos via CASE explícito.
        var items = await query
            .OrderBy(a => a.Severity == "critical" ? 0 : a.Severity == "high" ? 1 : a.Severity == "medium" ? 2 : a.Severity == "low" ? 3 : 4)
            .ThenByDescending(a => a.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<AlertTypeCount>> CountByAlertTypeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default) =>
        await context.SecurityAlerts
            .Where(a => !a.IsDeleted && a.CreatedAtUtc >= fromUtc && a.CreatedAtUtc <= toUtc)
            .GroupBy(a => a.AlertType)
            .Select(g => new AlertTypeCount(g.Key, g.Count()))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AlertSeverityCount>> CountOpenBySeverityAsync(CancellationToken ct = default) =>
        await context.SecurityAlerts
            .Where(a => !a.IsDeleted && a.Status == "open")
            .GroupBy(a => a.Severity)
            .Select(g => new AlertSeverityCount(g.Key, g.Count()))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AlertEnvironmentCount>> CountByEnvironmentAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default) =>
        await context.SecurityAlerts
            .Where(a => !a.IsDeleted && a.CreatedAtUtc >= fromUtc && a.CreatedAtUtc <= toUtc)
            .GroupBy(a => a.Environment)
            .Select(g => new AlertEnvironmentCount(g.Key, g.Count()))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AlertTimeSeriesPoint>> GetHourlyTimeSeriesAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        // Agregação por hora é feita em memória para manter compatibilidade entre
        // o provider real (Postgres) e o InMemory provider usado em testes.
        var rows = await context.SecurityAlerts
            .Where(a => !a.IsDeleted && a.CreatedAtUtc >= fromUtc && a.CreatedAtUtc <= toUtc)
            .Select(a => new { a.AlertType, a.CreatedAtUtc })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => (Bucket: new DateTime(r.CreatedAtUtc.Year, r.CreatedAtUtc.Month, r.CreatedAtUtc.Day, r.CreatedAtUtc.Hour, 0, 0, DateTimeKind.Utc), r.AlertType))
            .Select(g => new AlertTimeSeriesPoint(g.Key.Bucket, g.Key.AlertType, g.Count()))
            .OrderBy(p => p.BucketUtc)
            .ToList();
    }

    public async Task<IReadOnlyList<AlertOriginCount>> GetTopMaskedOriginsAsync(DateTime fromUtc, DateTime toUtc, int top, CancellationToken ct = default)
    {
        var rows = await context.SecurityAlerts
            .Where(a => !a.IsDeleted && a.CreatedAtUtc >= fromUtc && a.CreatedAtUtc <= toUtc && a.MaskedIp != null)
            .Select(a => new { a.MaskedIp })
            .ToListAsync(ct);
        return rows
            .GroupBy(a => a.MaskedIp!)
            .Select(g => new AlertOriginCount(g.Key, g.Count()))
            .OrderByDescending(o => o.Count)
            .Take(top)
            .ToList();
    }

    public async Task<IReadOnlyList<AlertAffectedUserCount>> CountByAffectedUserAsync(DateTime fromUtc, DateTime toUtc, int top, CancellationToken ct = default)
    {
        var rows = await context.SecurityAlerts
            .Where(a => !a.IsDeleted && a.CreatedAtUtc >= fromUtc && a.CreatedAtUtc <= toUtc && a.AffectedUserId != null)
            .Select(a => new { a.AffectedUserId })
            .ToListAsync(ct);
        return rows
            .GroupBy(a => a.AffectedUserId!.Value)
            .Select(g => new AlertAffectedUserCount(g.Key, g.Count()))
            .OrderByDescending(u => u.Count)
            .Take(top)
            .ToList();
    }

    public async Task<IReadOnlyList<AlertEndpointCount>> CountRateLimitByEndpointAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var rows = await context.SecurityAlerts
            .Where(a => !a.IsDeleted && a.AlertType == "rate_limit_hit" && a.CreatedAtUtc >= fromUtc && a.CreatedAtUtc <= toUtc && a.Origin != null)
            .Select(a => new { a.Origin })
            .ToListAsync(ct);
        return rows
            .GroupBy(a => a.Origin!)
            .Select(g => new AlertEndpointCount(g.Key, g.Count()))
            .OrderByDescending(e => e.Count)
            .ToList();
    }

    public async Task<IReadOnlyList<AlertEndpointCount>> CountRbacDeniedByResourceAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var rows = await context.SecurityAlerts
            .Where(a => !a.IsDeleted && a.AlertType == "rbac_denied" && a.CreatedAtUtc >= fromUtc && a.CreatedAtUtc <= toUtc && a.Origin != null)
            .Select(a => new { a.Origin })
            .ToListAsync(ct);
        return rows
            .GroupBy(a => a.Origin!)
            .Select(g => new AlertEndpointCount(g.Key, g.Count()))
            .OrderByDescending(e => e.Count)
            .ToList();
    }

    public async Task<int> CountByAlertTypeInWindowAsync(string alertType, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default) =>
        await context.SecurityAlerts
            .Where(a => !a.IsDeleted && a.AlertType == alertType && a.CreatedAtUtc >= fromUtc && a.CreatedAtUtc <= toUtc)
            .CountAsync(ct);

    public async Task<double?> GetAverageMinutesToAnalysisAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var analyzed = await context.SecurityAlerts
            .Where(a => !a.IsDeleted
                && a.AnalyzedAtUtc != null
                && a.CreatedAtUtc >= fromUtc
                && a.CreatedAtUtc <= toUtc)
            .Select(a => new { a.CreatedAtUtc, a.AnalyzedAtUtc })
            .ToListAsync(ct);

        if (analyzed.Count == 0) return null;

        return analyzed.Average(a => (a.AnalyzedAtUtc!.Value - a.CreatedAtUtc).TotalMinutes);
    }

    public async Task<bool> HasOpenRecentAlertAsync(string alertType, Guid? affectedUserId, DateTime sinceUtc, CancellationToken ct = default)
    {
        var query = context.SecurityAlerts.Where(a =>
            !a.IsDeleted &&
            a.AlertType == alertType &&
            a.Status == "open" &&
            a.CreatedAtUtc >= sinceUtc);

        query = affectedUserId.HasValue
            ? query.Where(a => a.AffectedUserId == affectedUserId.Value)
            : query.Where(a => a.AffectedUserId == null);

        return await query.AnyAsync(ct);
    }
}
