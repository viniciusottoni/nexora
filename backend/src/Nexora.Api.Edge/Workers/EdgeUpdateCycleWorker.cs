using Nexora.Application.Installations.Commands.RunEdgeUpdateCycle;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nexora.Api.Edge.Workers;

/// <summary>
/// Motor de atualização controlada do parque (US-146 §7) — varredura periódica que dispara
/// <see cref="RunEdgeUpdateCycleCommand"/>, o qual decide sozinho (dentro da janela configurada?
/// pendências de sincronização abaixo do limiar?) se deve agir. Mesmo esqueleto de
/// <c>AlertEvaluationWorker</c>/<c>WaiterCallEscalationWorker</c> — não itera tenants (edge é
/// single-tenant, ADR-004). ADR-019: só este worker, do lado edge, decide "quando agir" — a nuvem
/// nunca empurra a atualização.
/// </summary>
public sealed partial class EdgeUpdateCycleWorker : BackgroundService
{
    /// <summary>
    /// 15 minutos — fino o suficiente para não perder a janela de atualização de algumas horas
    /// (ex.: 4h-6h do cenário Gherkin) sem sobrecarregar o banco com uma varredura a cada tick de
    /// segundos; mesma ordem de grandeza de <c>InstallationHealthEvaluationWorker</c> (3 min) e
    /// <c>TenantDomainCertificateRenewalWorker</c>, ajustada para cima porque este ciclo pode
    /// disparar I/O pesado (backup) quando decide agir.
    /// </summary>
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EdgeUpdateCycleWorker> _logger;

    public EdgeUpdateCycleWorker(IServiceScopeFactory scopeFactory, ILogger<EdgeUpdateCycleWorker> logger)
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
    public async Task<RunEdgeUpdateCycleResult?> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(new RunEdgeUpdateCycleCommand(), cancellationToken);
        if (result.IsFailure)
        {
            LogCicloFalhou(result.Error);
            return null;
        }

        return result.Value;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao varrer o ciclo de atualização controlada do parque.")]
    private partial void LogVarreduraFalhou(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ciclo de atualização controlada retornou falha: {Erro}")]
    private partial void LogCicloFalhou(string? erro);
}
