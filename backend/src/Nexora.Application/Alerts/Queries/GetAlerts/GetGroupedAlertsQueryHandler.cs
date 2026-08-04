using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Alerts.Support;
using Nexora.Contracts.Alerts;
using Nexora.Domain.Metrics;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Alerts.Queries.GetAlerts;

internal sealed class GetGroupedAlertsQueryHandler : IRequestHandler<GetGroupedAlertsQuery, Result<AlertGroupListResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public GetGroupedAlertsQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<AlertGroupListResponse>> Handle(GetGroupedAlertsQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<AlertGroupListResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à sua sessão.", ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var open = await _db.Alerts.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.ResolvedAt == null)
            .OrderByDescending(a => a.RaisedAt)
            .ToListAsync(cancellationToken);

        // Alerta sem GroupKey (tipo sem agrupamento configurado, US-083 §3 "tipos distintos não
        // agrupam") vira um grupo de tamanho 1 com a própria mensagem individual — a central de
        // notificações precisa ver TODO alerta aberto por este mesmo endpoint.
        var groups = open
            .GroupBy(a => a.GroupKey ?? $"__single__:{a.Id}")
            .Select(BuildGroup)
            .OrderByDescending(g => g.LastRaisedAt)
            .ToList();

        return Result<AlertGroupListResponse>.Success(new AlertGroupListResponse(groups));
    }

    private static AlertGroupResponse BuildGroup(IGrouping<string, Alert> group)
    {
        var alerts = group.OrderByDescending(a => a.RaisedAt).ToList();
        var type = alerts[0].Type;
        var severity = alerts.Max(a => a.Severity);
        var message = alerts.Count > 1 ? AlertMessages.GroupMessage(type, alerts.Count) : alerts[0].Message;

        return new AlertGroupResponse(
            type,
            alerts.Count,
            severity.ToString().ToUpperInvariant(),
            message,
            alerts.Min(a => a.RaisedAt),
            alerts.Max(a => a.RaisedAt),
            alerts.Select(AlertMapper.ToResponse).ToList());
    }
}
