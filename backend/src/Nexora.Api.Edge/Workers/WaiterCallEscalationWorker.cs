using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Domain.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nexora.Api.Edge.Workers;

/// <summary>
/// Escalonamento de chamada de garçom não atendida (US-025 §4, cenário "Escalonamento por falta de
/// atendimento": "Dado uma chamada pendente há mais do que o limiar configurado, Quando o limiar for
/// ultrapassado, Então o alerta deve escalar para os demais garçons do ambiente") — réplica do MESMO
/// padrão de <see cref="AvailabilityAutoRestoreWorker"/> (varredura periódica via
/// <see cref="BackgroundService"/>, escopo novo por tick).
/// </summary>
/// <remarks>
/// [HIPÓTESE] <see cref="EscalationThreshold"/> é uma constante fixa — não existe
/// <c>tenant_config</c> para "limiar de escalonamento de chamada de garçom" nesta solution ainda, e
/// a US não especifica o valor (só pede "limiar configurado"). 3 minutos foi escolhido como ponto de
/// partida razoável para operação de salão; quando o campo de configuração existir, trocar a
/// constante por leitura de <c>TenantConfig.Operation</c> (mesmo padrão de
/// <c>BusinessDayPolicy.ResolveStartHourUtc</c>) sem mudar o resto do worker.
///
/// [DECISÃO] Diferente de <see cref="AvailabilityAutoRestoreWorker"/>, este worker NÃO despacha um
/// <c>ICommand</c> por tenant via <c>ISender</c> — consulta <c>Alert</c> diretamente. O edge tem
/// exatamente UM tenant fixo por instalação (ADR-004, <c>EdgeInstallationOptions</c>), e
/// <c>EdgeCurrentTenantContext.TenantId</c> não depende de <c>HttpContext</c> (lê direto da opção),
/// então o RLS (via <c>TenantConnectionInterceptor</c>) já filtra corretamente mesmo fora de uma
/// requisição HTTP — não há necessidade do loop "por tenant" que o gêmeo de disponibilidade tem
/// (aquele existe porque <c>RestoreProductsPastBusinessDayCommand</c> já era multi-tenant por
/// desenho, pensando no Cloud). Esta escalação é conceito exclusivo do edge (alerta operacional
/// local, RN-005) — não tem gêmeo em <c>Nexora.Api.Cloud</c>.
/// </remarks>
public sealed class WaiterCallEscalationWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    /// <summary>Papel escalado — todos os garçons do ambiente (Gherkin: "escalar para os demais garçons").</summary>
    private static readonly IReadOnlyList<string> EscalatedRoles = new[] { "WAITER" };

    public static readonly TimeSpan EscalationThreshold = TimeSpan.FromMinutes(3);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WaiterCallEscalationWorker> _logger;

    public WaiterCallEscalationWorker(IServiceScopeFactory scopeFactory, ILogger<WaiterCallEscalationWorker> logger)
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
                _logger.LogWarning(ex, "Falha ao varrer chamadas de garçom pendentes para escalonamento.");
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

    /// <summary>Processa uma varredura completa e retorna a contagem escalada — público para os testes exercitarem sem esperar o intervalo.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var broadcaster = scope.ServiceProvider.GetRequiredService<IAlertsBroadcaster>();

        var cutoff = DateTimeOffset.UtcNow - EscalationThreshold;

        var candidates = await db.Alerts
            .Where(a => a.Type == AlertTypes.WaiterCalled && a.ResolvedAt == null && a.RaisedAt <= cutoff)
            .ToListAsync(cancellationToken);

        // Filtro final em memória (não em SQL): TargetRoles é IReadOnlyList<string> mapeado como
        // text[] — .Contains sobre esse tipo não é garantidamente traduzível pelo provider Npgsql;
        // o volume de alertas pendentes é sempre pequeno (uma chamada de garçom não atendida é
        // exceção, não regra), então filtrar depois de trazer para memória é seguro aqui.
        var pending = candidates.Where(a => !a.TargetRoles.Contains("WAITER")).ToList();
        if (pending.Count == 0)
        {
            return 0;
        }

        foreach (var alert in pending)
        {
            alert.Escalate(EscalatedRoles);

            if (alert.EntityId is { } sessionId)
            {
                var session = await db.TableSessions
                    .AsNoTracking()
                    .Include(s => s.Table)
                    .SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

                if (session is not null)
                {
                    await broadcaster.WaiterCallEscalated(alert.TenantId, session.TableId, session.Table.Label, cancellationToken);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return pending.Count;
    }
}
