using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Alerts.Support;
using Nexora.Domain.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Alerts.Commands.EscalatePendingAlerts;

internal sealed class EscalatePendingAlertsCommandHandler : IRequestHandler<EscalatePendingAlertsCommand, Result<int>>
{
    /// <summary>US-082 §7 "escala... para o gestor" (ORDER_LATE, o único tipo do MVP com escalateAfterSeconds configurado por padrão, já inclui MANAGER — este papel é o piso de escalonamento para qualquer tipo).</summary>
    private const string EscalationRole = "MANAGER";

    private readonly IApplicationDbContext _db;
    private readonly IAlertsBroadcaster _broadcaster;

    public EscalatePendingAlertsCommandHandler(IApplicationDbContext db, IAlertsBroadcaster broadcaster)
    {
        _db = db;
        _broadcaster = broadcaster;
    }

    public async Task<Result<int>> Handle(EscalatePendingAlertsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId;
        await _db.SetTenantContextAsync(tenantId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var config = await _db.TenantConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        var routing = AlertRoutingConfig.Parse(config?.Operation);

        var pending = await _db.Alerts
            .Where(a => a.TenantId == tenantId && a.AcknowledgedAt == null && a.ResolvedAt == null)
            .ToListAsync(cancellationToken);

        var escalatedCount = 0;
        foreach (var alert in pending)
        {
            var rule = routing.Resolve(alert.Type);
            if (rule.EscalateAfterSeconds is not { } seconds || seconds <= 0)
            {
                continue;
            }

            if (now - alert.RaisedAt < TimeSpan.FromSeconds(seconds))
            {
                continue;
            }

            // Idempotente: só escala uma vez (papel já presente = já escalado nesta rodada anterior).
            if (alert.TargetRoles.Contains(EscalationRole, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var newRoles = alert.TargetRoles.Append(EscalationRole).ToArray();
            alert.Escalate(newRoles);
            await _broadcaster.AlertRaised(alert, cancellationToken);
            escalatedCount++;
        }

        return Result<int>.Success(escalatedCount);
    }
}
