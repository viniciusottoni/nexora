using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Catalog.Availability.Commands.RestoreProductsPastBusinessDay;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nexora.Api.Cloud.Workers;

/// <summary>
/// Retorno automático de produtos indisponíveis no início do próximo dia operacional (US-015 §3.1,
/// cenário "Retorno automático no novo dia operacional"; ADR-018 para a virada). Mesmo padrão de
/// <c>Nexora.Infrastructure.Notifications.EmailOutboxDeliveryWorker</c> — itera <c>tenant</c> (única
/// tabela sem RLS) e despacha, por tenant, <see cref="RestoreProductsPastBusinessDayCommand"/> via
/// <see cref="ISender"/> (que já fixa <c>app.tenant_id</c> explicitamente dentro do próprio handler,
/// via <see cref="IApplicationDbContext.SetTenantContextAsync"/> — este worker não precisa repetir
/// isso). Registrado só no Api.Cloud e no Api.Edge (ver gêmeo em <c>Nexora.Api.Edge.Workers</c>) —
/// os dois processos podem ter produtos indisponíveis pendentes de retorno (autoridade
/// bidirecional desta história).
///
/// Vive em <c>Api.Cloud</c>, não em <c>Infrastructure</c>: mantém o mesmo <see cref="BackgroundService"/>
/// simples que <c>EmailOutboxDeliveryWorker</c>/<c>SyncOutboxWorker</c> já usam, mas esta tarefa não
/// deveria editar arquivos de <c>Nexora.Infrastructure</c> (fora do escopo desta história) — por
/// isso o worker fica no projeto de Api, referenciando só <c>Nexora.Application</c>.
/// </summary>
public sealed partial class AvailabilityAutoRestoreWorker : BackgroundService
{
    /// <summary>
    /// Intervalo de varredura — não precisa ser fino: a virada do dia operacional acontece uma vez
    /// por dia (padrão 5h, ADR-018), então checar a cada 15 minutos já cobre a folga do cenário
    /// Gherkin sem sobrecarregar o banco. [PRÓXIMO PASSO] tornar configurável por
    /// `IConfiguration`/`IOptions` dedicado quando este módulo ganhar mais parâmetros.
    /// </summary>
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
                // Nunca deixa uma falha pontual (ex.: banco fora do ar) derrubar o worker — mesma
                // resiliência de EmailOutboxDeliveryWorker/SyncOutboxWorker.
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

    /// <summary>
    /// Processa uma varredura completa (todos os tenants) e retorna a contagem total restaurada —
    /// público de propósito, mesmo padrão de <c>EmailOutboxDeliveryWorker.RunOnceAsync</c>, usado
    /// pelos testes para exercitar uma iteração sem esperar o intervalo.
    /// </summary>
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
                LogRestauracaoFalhou(tenantId, result.Error);
            }
        }

        return restoredTotal;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao varrer produtos indisponíveis para retorno automático.")]
    private partial void LogVarreduraFalhou(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao restaurar produtos indisponíveis do tenant {TenantId}: {Erro}")]
    private partial void LogRestauracaoFalhou(Guid tenantId, string? erro);
}
