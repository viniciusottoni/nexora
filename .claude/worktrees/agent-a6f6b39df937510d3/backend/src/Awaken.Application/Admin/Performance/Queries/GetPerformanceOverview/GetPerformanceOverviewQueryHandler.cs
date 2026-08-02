using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Performance;
using Awaken.Shared.Admin;
using MediatR;

namespace Awaken.Application.Admin.Performance.Queries.GetPerformanceOverview;

/// <summary>
/// US-220: agrega saúde de banco/Redis, hit/miss de cache por domínio e métricas por rota crítica
/// para o painel admin de performance.
///
/// RN-002: cache sem métricas reais aparece como "no_data" — nunca "healthy" por omissão.
/// RN-004: banco ou Redis indisponível (status "critical" em qualquer um dos dois) derruba o
///         status geral do painel para "critical", independente do estado dos demais domínios.
/// RN-005: a resposta carrega apenas dados agregados (latências, status, contadores) — nenhum
///         payload de usuário é incluído.
/// </summary>
public class GetPerformanceOverviewQueryHandler(
    IPerformanceMetricsService performanceMetricsService,
    IDateTimeService dateTimeService)
    : IRequestHandler<GetPerformanceOverviewQuery, PerformanceOverviewResponse>
{
    public async Task<PerformanceOverviewResponse> Handle(
        GetPerformanceOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var database = await performanceMetricsService.GetDatabaseHealthAsync(cancellationToken);
        var redis = await performanceMetricsService.GetRedisHealthAsync(cancellationToken);
        var cacheDomains = performanceMetricsService.GetCacheDomainMetrics();
        var criticalRoutes = performanceMetricsService.GetCriticalRouteMetrics();
        var slowEndpoints = performanceMetricsService.GetSlowEndpoints();

        // RN-004: banco ou Redis indisponível/crítico derruba o painel inteiro para crítico,
        // mesmo que caches e rotas estejam sem dados (no_data não deve mascarar uma indisponibilidade real).
        string overallStatus;
        if (database.Status == DomainHealthStatus.Critical || redis.Status == DomainHealthStatus.Critical)
        {
            overallStatus = DomainHealthStatus.Critical;
        }
        else
        {
            var allStatuses = new List<string> { database.Status, redis.Status }
                .Concat(cacheDomains.Select(c => c.Status))
                .Concat(criticalRoutes.Select(r => r.Status));
            overallStatus = DomainHealthStatus.Aggregate(allStatuses);
        }

        return new PerformanceOverviewResponse(
            overallStatus,
            database,
            redis,
            cacheDomains,
            criticalRoutes,
            slowEndpoints,
            dateTimeService.UtcNow,
            request.Environment ?? "prod");
    }
}
