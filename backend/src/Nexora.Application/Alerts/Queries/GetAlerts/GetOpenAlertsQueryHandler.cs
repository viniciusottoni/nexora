using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Alerts.Support;
using Nexora.Contracts.Alerts;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Alerts.Queries.GetAlerts;

internal sealed class GetOpenAlertsQueryHandler : IRequestHandler<GetOpenAlertsQuery, Result<AlertListResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public GetOpenAlertsQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<AlertListResponse>> Handle(GetOpenAlertsQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<AlertListResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à sua sessão.", ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var limit = request.Limit is > 0 and <= 200 ? request.Limit : 100;

        // TargetRoles é text[] (IReadOnlyList<string>) — filtro de sobreposição de papéis não é
        // garantidamente traduzível pelo provider Npgsql (mesma decisão documentada em
        // WaiterCallEscalationWorker), então trazemos os alertas abertos do tenant (volume sempre
        // pequeno) e filtramos "é para mim" em memória.
        var candidates = await _db.Alerts.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.ResolvedAt == null)
            .OrderByDescending(a => a.RaisedAt)
            .Take(limit * 4)
            .ToListAsync(cancellationToken);

        IEnumerable<Domain.Metrics.Alert> filtered = candidates;
        if (request.OnlyForCurrentUser)
        {
            var userId = _tenantContext.UserId;
            var roles = new HashSet<string>(_tenantContext.Roles.Select(r => r.ToUpperInvariant()), StringComparer.Ordinal);

            filtered = candidates.Where(a =>
                (a.TargetUserId is { } targetUserId && userId == targetUserId)
                || a.TargetRoles.Any(r => roles.Contains(r.ToUpperInvariant())));
        }

        var alerts = filtered.Take(limit).Select(AlertMapper.ToResponse).ToList();
        return Result<AlertListResponse>.Success(new AlertListResponse(alerts, NextCursor: null));
    }
}
