using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Installations.Commands.EvaluateInstallationHealth;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nexora.Api.Cloud.Workers;

/// <summary>
/// Motor de saúde do painel de instalações (US-140 §9 "o painel é de nuvem... a detecção é da
/// nuvem, não do edge") — mesmo padrão de <c>AlertEvaluationWorker</c>: varre <c>tenant</c> e
/// despacha um comando por instalação via <see cref="ISender"/>, mantendo a lógica de transição
/// testável fora do <c>BackgroundService</c>.
/// </summary>
public sealed partial class InstallationHealthEvaluationWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(3);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InstallationHealthEvaluationWorker> _logger;

    public InstallationHealthEvaluationWorker(IServiceScopeFactory scopeFactory, ILogger<InstallationHealthEvaluationWorker> logger)
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

        var evaluatedTotal = 0;
        foreach (var tenantId in tenantIds)
        {
            // RLS (ADR-004): edge_installation tem tenant_isolation — sem este SET explícito
            // (worker não tem ICurrentTenantContext derivado de requisição HTTP), a leitura abaixo
            // não retornaria nenhuma linha.
            await db.SetTenantContextAsync(tenantId, cancellationToken);

            var installationIds = await db.EdgeInstallations.AsNoTracking()
                .Where(i => i.TenantId == tenantId && i.InstalledAt != null)
                .Select(i => i.Id)
                .ToListAsync(cancellationToken);

            foreach (var installationId in installationIds)
            {
                var evaluated = await sender.Send(new EvaluateInstallationHealthCommand(tenantId, installationId), cancellationToken);
                if (evaluated.IsSuccess)
                {
                    evaluatedTotal++;
                }
                else
                {
                    LogAvaliacaoFalhou(tenantId, installationId, evaluated.Error);
                }
            }
        }

        return evaluatedTotal;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao varrer saúde de instalações da plataforma.")]
    private partial void LogVarreduraFalhou(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao avaliar saúde da instalação {InstallationId} (tenant {TenantId}): {Erro}")]
    private partial void LogAvaliacaoFalhou(Guid tenantId, Guid installationId, string? erro);
}
