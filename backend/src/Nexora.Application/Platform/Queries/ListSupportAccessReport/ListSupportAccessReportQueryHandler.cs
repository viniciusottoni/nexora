using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Platform;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Platform.Queries.ListSupportAccessReport;

internal sealed class ListSupportAccessReportQueryHandler
    : IRequestHandler<ListSupportAccessReportQuery, Result<SupportAccessListResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListSupportAccessReportQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<SupportAccessListResponse>> Handle(
        ListSupportAccessReportQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var tenantIds = await ResolveTenantIdsAsync(request.TenantId, cancellationToken);

        var rows = new List<SupportAccessSummaryResponse>();
        foreach (var (tenantId, tenantName) in tenantIds)
        {
            // support_access tem RLS com USING — sem fixar o tenant explicitamente aqui, a
            // consulta abaixo não devolveria nenhuma linha (ADR-004, falha fechada). Mesmo
            // mecanismo de EmailOutboxDeliveryWorker.DeliverPendingForTenantAsync.
            await _db.SetTenantContextAsync(tenantId, cancellationToken);

            var query = _db.SupportAccesses.AsNoTracking().Where(a => a.TenantId == tenantId);

            if (request.From is { } from)
            {
                query = query.Where(a => a.GrantedAt >= from);
            }

            if (request.To is { } to)
            {
                query = query.Where(a => a.GrantedAt <= to);
            }

            var tenantRows = await query
                .OrderByDescending(a => a.GrantedAt)
                .Take(ListSupportAccessReportQuery.MaxRows)
                .Select(a => new SupportAccessSummaryResponse(
                    a.Id,
                    a.TenantId,
                    tenantName,
                    a.GrantedTo,
                    a.Reason,
                    a.DurationMinutes,
                    a.GrantedAt,
                    a.ExpiresAt,
                    a.RevokedAt,
                    a.RevokedBy,
                    a.LastUsedAt,
                    a.RevokedAt == null && a.ExpiresAt > now))
                .ToListAsync(cancellationToken);

            rows.AddRange(tenantRows);
        }

        var ordered = rows
            .OrderByDescending(r => r.GrantedAt)
            .Take(ListSupportAccessReportQuery.MaxRows)
            .ToList();

        return Result<SupportAccessListResponse>.Success(new SupportAccessListResponse(ordered));
    }

    private async Task<List<(Guid TenantId, string TenantName)>> ResolveTenantIdsAsync(
        Guid? tenantId, CancellationToken cancellationToken)
    {
        var query = _db.Tenants.AsNoTracking().Where(t => t.DeletedAt == null);

        if (tenantId is { } id)
        {
            query = query.Where(t => t.Id == id);
        }

        return (await query.Select(t => new { t.Id, t.Name }).ToListAsync(cancellationToken))
            .Select(t => (t.Id, t.Name))
            .ToList();
    }
}
