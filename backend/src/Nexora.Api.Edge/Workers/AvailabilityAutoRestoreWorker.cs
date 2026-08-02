using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Catalog.Availability.Commands.RestoreProductsPastBusinessDay;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nexora.Api.Edge.Workers;

/// <summary>
/// Retorno automático de produtos indisponíveis no início do próximo dia operacional (US-015 §3.1,
/// cenário "Retorno automático no novo dia operacional"; ADR-018 para a virada) — réplica do gêmeo
/// em <c>Nexora.Api.Cloud.Workers</c>. No edge, "uma loja = um tenant" (ADR-004): a lista de
/// tenants tem sempre um único item, mas o loop é mantido idêntico ao do cloud para não duplicar
/// lógica divergente entre os dois processos.
/// </summary>
public sealed class AvailabilityAutoRestoreWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AvailabilityAutoRestoreWorker> _logger;

    public AvailabilityAutoRestoreWorker(IServiceScopeFactory scopeFactory, ILogger<AvailabilityAutoRestoreWorker> logger)
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
                _logger.LogWarning(ex, "Falha ao varrer produtos indisponíveis para retorno automático.");
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

    /// <summary>Processa uma varredura completa e retorna a contagem restaurada — público para os testes exercitarem sem esperar o intervalo.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var tenantIds = await db.Tenants
            .AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var restoredTotal = 0;

        foreach (var tenantId in tenantIds)
        {
            var result = await sender.Send(new RestoreProductsPastBusinessDayCommand(tenantId), cancellationToken);
            if (result.IsSuccess)
            {
                restoredTotal += result.Value;
            }
            else
            {
                _logger.LogWarning(
                    "Falha ao restaurar produtos indisponíveis do tenant {TenantId}: {Erro}", tenantId, result.Error);
            }
        }

        return restoredTotal;
    }
}
