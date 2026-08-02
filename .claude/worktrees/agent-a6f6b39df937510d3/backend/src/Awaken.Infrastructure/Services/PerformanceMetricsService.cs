using System.Diagnostics;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Performance;
using Awaken.Infrastructure.Persistence;
using Awaken.Shared.Admin;
using StackExchange.Redis;

namespace Awaken.Infrastructure.Services;

/// <summary>
/// US-220: coleta sinais de performance operacional para o painel admin.
///
/// LIMITAÇÃO CONHECIDA E DOCUMENTADA: este serviço mede latência real de PostgreSQL (ping via
/// CanConnectAsync + Stopwatch) e Redis (PingAsync via IConnectionMultiplexer). Porém, no estado
/// atual do projeto NÃO existe nenhuma fonte real de contadores de hit/miss de cache (ICacheService,
/// IAccessStatusCacheService, IExerciseCatalogCacheService e IShopProductCacheService não expõem
/// telemetria) nem agregação de métricas por rota (sem OpenTelemetry/APM persistido e consultável —
/// US-213 ainda não entrega essa fonte). Por isso, hit/miss de cache, p95/p99/erro/RPS por rota e
/// a lista de endpoints lentos retornam "no_data" honestamente (RN-002), em vez de simular números.
/// Quando essas fontes existirem, basta trocar os métodos abaixo para ler da fonte real — o contrato
/// público (IPerformanceMetricsService) já está pronto para isso.
/// </summary>
public class PerformanceMetricsService(
    AwakenDbContext dbContext,
    IConnectionMultiplexer redis) : IPerformanceMetricsService
{
    // Thresholds documentados (RN-EPIC-017-018): abaixo de "Attention" é Healthy.
    // Acima de "Critical" o domínio correspondente derruba o status geral (RN-004).
    private const double DatabaseAttentionMs = 150;
    private const double DatabaseCriticalMs = 500;
    private const double RedisAttentionMs = 50;
    private const double RedisCriticalMs = 200;

    private static readonly string[] CacheDomains =
    [
        "access_status",
        "exercise_catalog",
        "shop_products",
    ];

    private static readonly string[] CriticalRoutes =
    [
        "POST /api/quests/{id}/complete",
        "GET /api/progression/me",
        "POST /api/auth/login",
    ];

    public async Task<DatabaseHealthResponse> GetDatabaseHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        bool canConnect;
        try
        {
            canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            canConnect = false;
        }
        stopwatch.Stop();

        if (!canConnect)
        {
            // RN-004: banco indisponível deixa o painel crítico.
            return new DatabaseHealthResponse(DomainHealthStatus.Critical, null);
        }

        var latencyMs = stopwatch.Elapsed.TotalMilliseconds;
        var status = latencyMs switch
        {
            >= DatabaseCriticalMs => DomainHealthStatus.Critical,
            >= DatabaseAttentionMs => DomainHealthStatus.Attention,
            _ => DomainHealthStatus.Healthy,
        };

        return new DatabaseHealthResponse(status, latencyMs);
    }

    public async Task<RedisHealthResponse> GetRedisHealthAsync(CancellationToken cancellationToken = default)
    {
        if (!redis.IsConnected)
        {
            // RN-004: Redis indisponível deixa o painel crítico.
            return new RedisHealthResponse(DomainHealthStatus.Critical, null, false);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var db = redis.GetDatabase();
            await db.PingAsync();
            stopwatch.Stop();
        }
        catch (RedisException)
        {
            return new RedisHealthResponse(DomainHealthStatus.Critical, null, false);
        }

        var latencyMs = stopwatch.Elapsed.TotalMilliseconds;
        var status = latencyMs switch
        {
            >= RedisCriticalMs => DomainHealthStatus.Critical,
            >= RedisAttentionMs => DomainHealthStatus.Attention,
            _ => DomainHealthStatus.Healthy,
        };

        return new RedisHealthResponse(status, latencyMs, true);
    }

    public IReadOnlyList<CacheDomainMetricsResponse> GetCacheDomainMetrics()
    {
        // RN-002: nenhuma fonte real de hit/miss existe hoje — reporta no_data honestamente
        // para cada domínio em vez de inventar uma taxa de acerto.
        return CacheDomains
            .Select(domain => new CacheDomainMetricsResponse(domain, DomainHealthStatus.NoData, null, null, null))
            .ToList();
    }

    public IReadOnlyList<RouteMetricsResponse> GetCriticalRouteMetrics()
    {
        // RN-001/RN-002: sem OpenTelemetry/APM persistido e consultável no momento, reporta
        // no_data para p95/p99/erro/RPS em vez de simular dados falsos.
        return CriticalRoutes
            .Select(route => new RouteMetricsResponse(route, DomainHealthStatus.NoData, null, null, null, null, null))
            .ToList();
    }

    public IReadOnlyList<SlowEndpointResponse> GetSlowEndpoints()
    {
        // Sem fonte de métricas agregadas por rota, a lista de endpoints lentos vem vazia.
        return [];
    }
}
