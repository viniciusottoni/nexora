using Awaken.Contracts.Admin.Performance;

namespace Awaken.Application.Common.Interfaces;

/// <summary>
/// US-220: coleta sinais de performance operacional para o painel admin (banco, Redis, caches, rotas).
/// Implementações devem reportar "no_data" honestamente quando não houver fonte real disponível,
/// em vez de inventar números (RN-002).
/// </summary>
public interface IPerformanceMetricsService
{
    Task<DatabaseHealthResponse> GetDatabaseHealthAsync(CancellationToken cancellationToken = default);

    Task<RedisHealthResponse> GetRedisHealthAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<CacheDomainMetricsResponse> GetCacheDomainMetrics();

    IReadOnlyList<RouteMetricsResponse> GetCriticalRouteMetrics();

    IReadOnlyList<SlowEndpointResponse> GetSlowEndpoints();
}
