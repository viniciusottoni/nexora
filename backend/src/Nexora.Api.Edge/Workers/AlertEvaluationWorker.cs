using Nexora.Application.Abstractions.Security;
using Nexora.Application.Alerts.Commands.EscalatePendingAlerts;
using Nexora.Application.Alerts.Commands.EvaluateEdgeAlertConditions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nexora.Api.Edge.Workers;

/// <summary>
/// Motor de alertas do edge (E-08/US-080 §9 "o motor roda no edge para os alertas operacionais") —
/// varredura periódica que avalia o catálogo do MVP (<see cref="EvaluateEdgeAlertConditionsCommand"/>)
/// e escalona quem não recebeu resposta a tempo (<see cref="EscalatePendingAlertsCommand"/>, US-082
/// §4). Mesmo esqueleto de <c>WaiterCallEscalationWorker</c>/<c>AvailabilityAutoRestoreWorker</c> —
/// não itera tenants (edge é single-tenant, ADR-004).
/// </summary>
public sealed partial class AlertEvaluationWorker : BackgroundService
{
    /// <summary>1 minuto — fino o suficiente para o cenário Gherkin "Pedido atrasado" (limiares em minutos) sem sobrecarregar o banco a cada tick.</summary>
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

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

    /// <summary>Público — usado pelos testes para exercitar uma iteração sem esperar o intervalo.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ICurrentTenantContext>();

        var evaluated = await sender.Send(new EvaluateEdgeAlertConditionsCommand(), cancellationToken);
        var raisedCount = evaluated.IsSuccess ? evaluated.Value : 0;

        if (tenantContext.TenantId is { } tenantId)
        {
            var escalated = await sender.Send(new EscalatePendingAlertsCommand(tenantId), cancellationToken);
            if (escalated.IsFailure)
            {
                LogEscalonamentoFalhou(escalated.Error);
            }
        }

        return raisedCount;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao varrer condições de alerta do edge.")]
    private partial void LogVarreduraFalhou(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao escalonar alertas pendentes: {Erro}")]
    private partial void LogEscalonamentoFalhou(string? erro);
}
