using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Audit.Support;
using Nexora.Contracts.Audit;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Audit.Queries.GetAuditLog;

internal sealed class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, Result<AuditLogListResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public GetAuditLogQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<AuditLogListResponse>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is not { } tenantId)
        {
            return Result<AuditLogListResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        // MinAmount não tem coluna tipada própria (Before/After são JSONB livre) — resolvido via
        // IApplicationDbContext.AuditLogsWithMinAmount (implementado em Infrastructure, que pode
        // usar SQL específico do provider; Application não pode, ADR-039). O restante dos filtros
        // compõe normalmente por cima (a consulta base vira subconsulta, LINQ adicional traduz).
        IQueryable<AuditLog> query = request.MinAmount is { } minAmount
            ? _db.AuditLogsWithMinAmount(minAmount)
            : _db.AuditLogs;

        query = query.AsNoTracking().Where(a => a.TenantId == tenantId);

        if (request.From is { } from)
        {
            query = query.Where(a => a.OccurredAt >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(a => a.OccurredAt <= to);
        }

        if (request.ActorId is { } actorId)
        {
            query = query.Where(a => a.ActorId == actorId);
        }

        if (request.AuthorizedBy is { } authorizedBy)
        {
            query = query.Where(a => a.AuthorizedBy == authorizedBy);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(a => a.Action == request.Action);
        }

        if (!string.IsNullOrWhiteSpace(request.Entity))
        {
            query = query.Where(a => a.Entity == request.Entity);
        }

        if (request.EntityId is { } entityId)
        {
            query = query.Where(a => a.EntityId == entityId);
        }

        if (AuditLogCursor.Decode(request.Cursor) is { } cursor)
        {
            query = query.Where(a =>
                a.OccurredAt < cursor.OccurredAt ||
                (a.OccurredAt == cursor.OccurredAt && a.Id.CompareTo(cursor.Id) < 0));
        }

        var page = await query
            .OrderByDescending(a => a.OccurredAt)
            .ThenByDescending(a => a.Id)
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > request.Limit;
        var rows = hasMore ? page.Take(request.Limit).ToList() : page;

        var data = await BuildEntriesAsync(tenantId, rows, cancellationToken);

        var nextCursor = hasMore
            ? AuditLogCursor.Encode(rows[^1].OccurredAt, rows[^1].Id)
            : null;

        return Result<AuditLogListResponse>.Success(
            new AuditLogListResponse(data, new AuditLogListMeta(nextCursor, hasMore)));
    }

    private async Task<IReadOnlyList<AuditLogEntryResponse>> BuildEntriesAsync(
        Guid tenantId, IReadOnlyList<AuditLog> rows, CancellationToken cancellationToken)
    {
        var userIds = rows.SelectMany(r => new[] { r.ActorId, r.AuthorizedBy })
            .Where(id => id is not null).Select(id => id!.Value).Distinct().ToList();
        var deviceIds = rows.Select(r => r.DeviceId).Where(id => id is not null).Select(id => id!.Value).Distinct().ToList();

        var userNames = userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name }).ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var deviceLabels = deviceIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Devices.AsNoTracking().Where(d => deviceIds.Contains(d.Id))
                .Select(d => new { d.Id, d.Label }).ToDictionaryAsync(d => d.Id, d => d.Label, cancellationToken);

        var targetLabels = await ResolveTargetLabelsAsync(tenantId, rows, cancellationToken);

        return rows.Select(r =>
        {
            // Parse único por linha — antes cada Before/After era desserializado duas vezes
            // (uma para o resumo, outra para o payload da resposta).
            var beforeJson = AuditSummaryFormatter.TryParseJson(r.Before);
            var afterJson = AuditSummaryFormatter.TryParseJson(r.After);

            return new AuditLogEntryResponse(
                r.Id,
                r.Action,
                AuditSummaryFormatter.Format(r.Action, beforeJson, afterJson),
                r.ActorId is { } actorId ? new AuditActorSummary(actorId, userNames.GetValueOrDefault(actorId, "Usuário removido")) : null,
                r.AuthorizedBy is { } authorizedBy ? new AuditActorSummary(authorizedBy, userNames.GetValueOrDefault(authorizedBy, "Usuário removido")) : null,
                r.DeviceId is { } deviceId ? new AuditDeviceSummary(deviceId, deviceLabels.GetValueOrDefault(deviceId, "Dispositivo removido")) : null,
                r.OccurredAt,
                r.EntityId is { } entityId ? new AuditTargetSummary(r.Entity, entityId, targetLabels.GetValueOrDefault((r.Entity, entityId), DefaultTargetLabel(r.Entity, entityId))) : null,
                beforeJson,
                afterJson,
                r.Reason,
                r.TraceId);
        })
        .ToList();
    }

    /// <summary>
    /// Rótulo legível de navegação (US-091 §3/§7) — resolvido só para os tipos de entidade com
    /// lookup barato e comum nas ações hoje auditadas (pedido, papel, estabelecimento). Outro tipo
    /// cai no rótulo genérico de <see cref="DefaultTargetLabel"/>; a US não exige cobrir todo tipo
    /// de entidade possível, só permitir a navegação (o Id sempre está presente no <c>target</c>).
    /// </summary>
    private async Task<Dictionary<(string Entity, Guid Id), string>> ResolveTargetLabelsAsync(
        Guid tenantId, IReadOnlyList<AuditLog> rows, CancellationToken cancellationToken)
    {
        var labels = new Dictionary<(string, Guid), string>();

        await AddLabelsAsync(labels, "order", DistinctEntityIds(rows, "order"), async ids =>
            (await _db.Orders.AsNoTracking().Where(o => o.TenantId == tenantId && ids.Contains(o.Id))
                .Select(o => new { o.Id, o.ShortCode }).ToListAsync(cancellationToken))
                .ToDictionary(o => o.Id, o => $"Pedido {o.ShortCode}"));

        await AddLabelsAsync(labels, "role", DistinctEntityIds(rows, "role"), async ids =>
            (await _db.Roles.AsNoTracking().Where(r => r.TenantId == tenantId && ids.Contains(r.Id))
                .Select(r => new { r.Id, r.Name }).ToListAsync(cancellationToken))
                .ToDictionary(r => r.Id, r => r.Name));

        await AddLabelsAsync(labels, "tenant", DistinctEntityIds(rows, "tenant"), async ids =>
            (await _db.Tenants.AsNoTracking().Where(t => ids.Contains(t.Id))
                .Select(t => new { t.Id, t.Name }).ToListAsync(cancellationToken))
                .ToDictionary(t => t.Id, t => t.Name));

        return labels;
    }

    private static List<Guid> DistinctEntityIds(IReadOnlyList<AuditLog> rows, string entity) =>
        rows.Where(r => r.Entity == entity && r.EntityId is not null).Select(r => r.EntityId!.Value).Distinct().ToList();

    private static async Task AddLabelsAsync(
        Dictionary<(string Entity, Guid Id), string> labels,
        string entity,
        IReadOnlyList<Guid> ids,
        Func<IReadOnlyList<Guid>, Task<Dictionary<Guid, string>>> loadNames)
    {
        if (ids.Count == 0)
        {
            return;
        }

        foreach (var (id, name) in await loadNames(ids))
        {
            labels[(entity, id)] = name;
        }
    }

    private static string DefaultTargetLabel(string entity, Guid id) => $"{entity} {id.ToString()[..8]}";
}
