using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Alerts.Support;
using Nexora.Domain.Cashier;
using Nexora.Domain.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Alerts.Commands.EvaluateCloudAlertConditions;

internal sealed class EvaluateCloudAlertConditionsCommandHandler : IRequestHandler<EvaluateCloudAlertConditionsCommand, Result<int>>
{
    /// <summary>Não reabre sessões fechadas há tempo demais — só a divergência recente é acionável.</summary>
    private static readonly TimeSpan CashDivergenceLookback = TimeSpan.FromHours(24);

    private readonly IApplicationDbContext _db;
    private readonly IAlertRaiser _raiser;

    public EvaluateCloudAlertConditionsCommandHandler(IApplicationDbContext db, IAlertRaiser raiser)
    {
        _db = db;
        _raiser = raiser;
    }

    public async Task<Result<int>> Handle(EvaluateCloudAlertConditionsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId;

        // RLS (ADR-004): este comando roda fora de requisição HTTP (BackgroundService, um tenant
        // por vez) — fixa o contexto explicitamente, mesmo mecanismo de RestoreProductsPastBusinessDayCommand.
        await _db.SetTenantContextAsync(tenantId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var config = await _db.TenantConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        var thresholds = AlertThresholds.Parse(config?.Thresholds);

        var raisedCount = 0;
        raisedCount += await EvaluateCashDivergenceAsync(tenantId, now, thresholds, cancellationToken);
        raisedCount += await EvaluateSyncDelayAsync(tenantId, now, thresholds, cancellationToken);

        return Result<int>.Success(raisedCount);
    }

    /// <summary>US-080 §2 "divergência de caixa" — US-082 §4 "alerta exclusivo de gestão" (a matriz padrão de ORDER_TYPES já restringe CASH_DIVERGENCE a MANAGER).</summary>
    private async Task<int> EvaluateCashDivergenceAsync(Guid tenantId, DateTimeOffset now, AlertThresholds thresholds, CancellationToken cancellationToken)
    {
        var windowStart = now - CashDivergenceLookback;

        var sessions = await _db.CashSessions.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.Status == CashSessionStatus.Closed
                        && s.ClosedAt != null && s.ClosedAt >= windowStart && s.Divergence != null)
            .ToListAsync(cancellationToken);

        var raisedCount = 0;
        foreach (var session in sessions)
        {
            var divergence = session.Divergence!.Value;
            if (Math.Abs(divergence) < thresholds.CashDivergenceAlert)
            {
                continue;
            }

            await _raiser.RaiseAsync(new RaiseAlertRequest(
                tenantId, session.StoreId, AlertTypes.CashDivergence, AlertSeverity.High,
                $"Sessão de caixa fechada com divergência de R$ {divergence:F2}.",
                "cash_session", session.Id), cancellationToken);
            raisedCount++;
        }

        return raisedCount;
    }

    /// <summary>US-080 §2 "falha de sincronização" — instalação edge sem contato recente com a nuvem.</summary>
    private async Task<int> EvaluateSyncDelayAsync(Guid tenantId, DateTimeOffset now, AlertThresholds thresholds, CancellationToken cancellationToken)
    {
        var installations = await _db.EdgeInstallations.AsNoTracking()
            .Where(i => i.TenantId == tenantId && i.InstalledAt != null)
            .ToListAsync(cancellationToken);

        var raisedCount = 0;
        foreach (var installation in installations)
        {
            var delayed = installation.LastSeenAt is null
                || now - installation.LastSeenAt.Value > TimeSpan.FromMinutes(thresholds.SyncDelayMinutes);

            if (delayed)
            {
                var minutesSince = installation.LastSeenAt is { } lastSeen ? (int)(now - lastSeen).TotalMinutes : -1;
                var message = minutesSince >= 0
                    ? $"Instalação \"{installation.Label}\" sem sincronizar há {minutesSince} minutos."
                    : $"Instalação \"{installation.Label}\" nunca sincronizou com a nuvem.";

                await _raiser.RaiseAsync(new RaiseAlertRequest(
                    tenantId, installation.StoreId, AlertTypes.SyncDelay, AlertSeverity.High, message,
                    "edge_installation", installation.Id), cancellationToken);
                raisedCount++;
            }
            else
            {
                await _raiser.ResolveAsync(tenantId, AlertTypes.SyncDelay, "edge_installation", installation.Id, cancellationToken);
            }
        }

        return raisedCount;
    }
}
