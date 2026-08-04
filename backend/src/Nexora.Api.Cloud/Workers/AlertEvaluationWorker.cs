using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Alerts.Commands.EscalatePendingAlerts;
using Nexora.Application.Alerts.Commands.EvaluateCloudAlertConditions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nexora.Api.Cloud.Workers;

/// <summary>
/// Motor de alertas de gestão da nuvem (E-08/US-080 §9 "alertas de gestão... rodam na nuvem":
/// CASH_DIVERGENCE, SYNC_DELAY) e escalonamento por falta de resposta (US-082 §4) — mesmo padrão de
/// <c>AvailabilityAutoRestoreWorker</c>: itera <c>tenant</c> e despacha por tenant via <see cref="ISender"/>.
/// </summary>
public sealed partial class AlertEvaluationWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertEvaluationWorker> _logger;

    public AlertEvaluationWorker(IServiceScopeFactory scopeFactory, ILogger<AlertEvaluationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogVarreduraFalhou(ex);
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var tenantIds = await db.Tenants.AsNoTracking().Where(t => t.DeletedAt == null).Select(t => t.Id).ToListAsync(cancellationToken);

        var raisedTotal = 0;
        foreach (var tenantId in tenantIds)
        {
            var evaluated = await sender.Send(new EvaluateCloudAlertConditionsCommand(tenantId), cancellationToken);
            if (evaluated.IsSuccess)
            {
                raisedTotal += evaluated.Value;
            }
            else
            {
                LogAvaliacaoFalhou(tenantId, evaluated.Error);
            }

            var escalated = await sender.Send(new EscalatePendingAlertsCommand(tenantId), cancellationToken);
            if (escalated.IsFailure)
            {
                LogEscalonamentoFalhou(tenantId, escalated.Error);
            }
        }

        return raisedTotal;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao varrer condições de alerta da nuvem.")]
    private partial void LogVarreduraFalhou(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao avaliar alertas do tenant {TenantId}: {Erro}")]
    private partial void LogAvaliacaoFalhou(Guid tenantId, string? erro);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao escalonar alertas do tenant {TenantId}: {Erro}")]
    private partial void LogEscalonamentoFalhou(Guid tenantId, string? erro);
}
