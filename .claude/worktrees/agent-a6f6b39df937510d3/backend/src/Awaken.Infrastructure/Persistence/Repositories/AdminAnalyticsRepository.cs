using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

/// <summary>
/// EPIC-017 — US-161/US-168/US-169: implementação somente-leitura das consultas
/// agregadas administrativas. Acessa AwakenDbContext diretamente pois não há
/// mutação de estado — apenas projeções/agregações sobre dados existentes.
/// </summary>
public class AdminAnalyticsRepository(AwakenDbContext context) : IAdminAnalyticsRepository
{
    public Task<int> CountUsersAsync(CancellationToken ct = default) =>
        context.Users.CountAsync(ct);

    public Task<int> CountDistinctActiveUsersSinceAsync(DateTime sinceUtc, CancellationToken ct = default) =>
        context.AuditLogs
            .Where(a => a.ActorType == AuditActorType.User && a.CreatedAtUtc >= sinceUtc && a.ActorUserId != null)
            .Select(a => a.ActorUserId!.Value)
            .Distinct()
            .CountAsync(ct);

    public Task<int> CountOpenSupportTicketsAsync(CancellationToken ct = default) =>
        context.SupportTickets.CountAsync(t => t.Status == "open", ct);

    public async Task<IReadOnlyList<(DateOnly Date, int Count)>> GetDailyActiveUsersAsync(
        DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var rows = await context.AuditLogs
            .Where(a => a.ActorType == AuditActorType.User
                        && a.ActorUserId != null
                        && a.CreatedAtUtc >= fromUtc
                        && a.CreatedAtUtc <= toUtc)
            .Select(a => new { a.CreatedAtUtc, a.ActorUserId })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => DateOnly.FromDateTime(r.CreatedAtUtc))
            .Select(g => (g.Key, g.Select(r => r.ActorUserId!.Value).Distinct().Count()))
            .OrderBy(g => g.Item1)
            .ToList();
    }

    public async Task<IReadOnlyList<(string Action, string ResourceType, DateTime CreatedAtUtc)>> GetRecentActivityAsync(
        int take, CancellationToken ct = default)
    {
        var rows = await context.AuditLogs
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(take)
            .Select(a => new { a.Action, a.ResourceType, a.CreatedAtUtc })
            .ToListAsync(ct);

        return rows.Select(r => (r.Action, r.ResourceType, r.CreatedAtUtc)).ToList();
    }

    public async Task<IReadOnlyList<(string Action, int Count)>> GetTopEventsAsync(
        DateTime fromUtc, DateTime toUtc, int take, CancellationToken ct = default)
    {
        var rows = await context.AuditLogs
            .Where(a => a.ActorType == AuditActorType.User && a.CreatedAtUtc >= fromUtc && a.CreatedAtUtc <= toUtc)
            .GroupBy(a => a.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(take)
            .ToListAsync(ct);

        return rows.Select(r => (r.Action, r.Count)).ToList();
    }

    public async Task<IReadOnlyList<string>> GetAllDistinctUserActionsAsync(CancellationToken ct = default) =>
        await context.AuditLogs
            .Where(a => a.ActorType == AuditActorType.User)
            .Select(a => a.Action)
            .Distinct()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<(string Action, int Count)>> GetEventVolumesAsync(
        DateTime fromUtc, DateTime toUtc, string? eventNameFilter, CancellationToken ct = default)
    {
        var query = context.AuditLogs
            .Where(a => a.ActorType == AuditActorType.User && a.CreatedAtUtc >= fromUtc && a.CreatedAtUtc <= toUtc);

        if (!string.IsNullOrWhiteSpace(eventNameFilter))
            query = query.Where(a => a.Action.Contains(eventNameFilter));

        var rows = await query
            .GroupBy(a => a.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.Select(r => (r.Action, r.Count)).ToList();
    }

    public async Task<IReadOnlyList<(DateOnly Date, string Action, int Count)>> GetEventTimeSeriesAsync(
        DateTime fromUtc, DateTime toUtc, IReadOnlyCollection<string>? eventNames, CancellationToken ct = default)
    {
        var query = context.AuditLogs
            .Where(a => a.ActorType == AuditActorType.User && a.CreatedAtUtc >= fromUtc && a.CreatedAtUtc <= toUtc);

        if (eventNames is { Count: > 0 })
            query = query.Where(a => eventNames.Contains(a.Action));

        var rows = await query
            .Select(a => new { a.CreatedAtUtc, a.Action })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => (DateOnly.FromDateTime(r.CreatedAtUtc), r.Action))
            .Select(g => (g.Key.Item1, g.Key.Action, g.Count()))
            .OrderBy(g => g.Item1)
            .ToList();
    }

    public async Task<IReadOnlyList<(Guid UserId, DateTime CreatedAtUtc)>> GetUsersRegisteredBeforeAsync(
        DateTime thresholdUtc, CancellationToken ct = default)
    {
        var rows = await context.Users
            .Where(u => u.CreatedAtUtc <= thresholdUtc)
            .Select(u => new { u.Id, u.CreatedAtUtc })
            .ToListAsync(ct);

        return rows.Select(r => (r.Id, r.CreatedAtUtc)).ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetUserIdsWithActivityAfterOffsetAsync(
        IReadOnlyCollection<Guid> userIds, int offsetDays, CancellationToken ct = default)
    {
        if (userIds.Count == 0) return [];

        var users = await context.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.CreatedAtUtc })
            .ToListAsync(ct);

        var thresholds = users.ToDictionary(u => u.Id, u => u.CreatedAtUtc.AddDays(offsetDays));

        var activity = await context.AuditLogs
            .Where(a => a.ActorType == AuditActorType.User
                        && a.ActorUserId != null
                        && userIds.Contains(a.ActorUserId.Value))
            .Select(a => new { ActorUserId = a.ActorUserId!.Value, a.CreatedAtUtc })
            .ToListAsync(ct);

        return activity
            .Where(a => thresholds.TryGetValue(a.ActorUserId, out var threshold) && a.CreatedAtUtc > threshold)
            .Select(a => a.ActorUserId)
            .Distinct()
            .ToList();
    }

    public async Task<IReadOnlyList<(string Action, int Count)>> GetFeatureUsageAsync(
        DateTime fromUtc, DateTime toUtc, int take, CancellationToken ct = default)
    {
        var rows = await context.AuditLogs
            .Where(a => a.ActorType == AuditActorType.User && a.CreatedAtUtc >= fromUtc && a.CreatedAtUtc <= toUtc)
            .GroupBy(a => a.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(take)
            .ToListAsync(ct);

        return rows.Select(r => (r.Action, r.Count)).ToList();
    }
}
