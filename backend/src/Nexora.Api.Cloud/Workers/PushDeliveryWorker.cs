using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Alerts.Commands.DeliverPendingPush;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nexora.Api.Cloud.Workers;

/// <summary>
/// US-081 §2 "o push é enviado pela nuvem" — varredura curta (alertas alta/crítica pendentes de
/// push são raros e o valor da entrega cai rápido com o atraso, RF-ALT-03) que despacha
/// <see cref="DeliverPendingPushCommand"/> por tenant, mesmo padrão de iteração de <c>AlertEvaluationWorker</c>.
/// </summary>
public sealed partial class PushDeliveryWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PushDeliveryWorker> _logger;

    public PushDeliveryWorker(IServiceScopeFactory scopeFactory, ILogger<PushDeliveryWorker> logger)
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

        var sentTotal = 0;
        foreach (var tenantId in tenantIds)
        {
            var result = await sender.Send(new DeliverPendingPushCommand(tenantId), cancellationToken);
            if (result.IsSuccess)
            {
                sentTotal += result.Value;
            }
            else
            {
                LogEntregaFalhou(tenantId, result.Error);
            }
        }

        return sentTotal;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao varrer push pendente.")]
    private partial void LogVarreduraFalhou(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao entregar push do tenant {TenantId}: {Erro}")]
    private partial void LogEntregaFalhou(Guid tenantId, string? erro);
}
