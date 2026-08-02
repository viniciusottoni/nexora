using Awaken.Application.Admin.Performance.Queries.GetPerformanceOverview;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Performance;
using Awaken.Shared.Admin;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.Performance;

/// <summary>
/// US-220 — testes do handler de visão geral de performance.
///
/// CA: rota saudável (banco+redis healthy, caches/rotas sem dados) -> overall não é Critical,
///     e como há domínios "no_data" sem nenhum "healthy" isolado restante, o agregado reflete
///     a regra "worst-of" de DomainHealthStatus.Aggregate.
/// CA: erro elevado / rota lenta -> refletido via status "attention"/"critical" nas RouteMetrics.
/// CA: cache hit alto/baixo -> ainda assim no_data nesta implementação (RN-002), nunca inventa número.
/// CA: Redis indisponível -> overall Critical (RN-004), mesmo com banco saudável.
/// CA: banco com latência alta -> overall Critical/Attention conforme status retornado (RN-004).
/// </summary>
public class GetPerformanceOverviewQueryHandlerTests
{
    private readonly Mock<IPerformanceMetricsService> _metricsService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private static readonly DateTime UtcNow = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);

    public GetPerformanceOverviewQueryHandlerTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);

        // Defaults: healthy database/redis, no_data caches/routes, empty slow endpoints.
        _metricsService.Setup(m => m.GetDatabaseHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DatabaseHealthResponse(DomainHealthStatus.Healthy, 5.0));
        _metricsService.Setup(m => m.GetRedisHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RedisHealthResponse(DomainHealthStatus.Healthy, 2.0, true));
        _metricsService.Setup(m => m.GetCacheDomainMetrics())
            .Returns([new CacheDomainMetricsResponse("access_status", DomainHealthStatus.NoData, null, null, null)]);
        _metricsService.Setup(m => m.GetCriticalRouteMetrics())
            .Returns([new RouteMetricsResponse("GET /api/progression/me", DomainHealthStatus.NoData, null, null, null, null, null)]);
        _metricsService.Setup(m => m.GetSlowEndpoints())
            .Returns([]);
    }

    private GetPerformanceOverviewQueryHandler CreateHandler() => new(_metricsService.Object, _dateTimeService.Object);

    [Fact]
    public async Task Handle_HealthyRoute_ReturnsNoDataOverall_WhenOnlyNoDataAndHealthyDomainsExist()
    {
        // banco/redis healthy + caches/rotas no_data -> Aggregate() = NoData só se TODOS forem no_data,
        // mas banco/redis são healthy, então o agregado deve ser Healthy (worst-of não tem critical/attention).
        var result = await CreateHandler().Handle(new GetPerformanceOverviewQuery(null, null, null), CancellationToken.None);

        result.OverallStatus.Should().Be(DomainHealthStatus.Healthy);
        result.Database.Status.Should().Be(DomainHealthStatus.Healthy);
        result.Redis.Status.Should().Be(DomainHealthStatus.Healthy);
        result.CacheDomains.Should().OnlyContain(c => c.Status == DomainHealthStatus.NoData);
        result.LastCollectedAtUtc.Should().Be(UtcNow);
    }

    [Fact]
    public async Task Handle_WhenRouteHasHighErrorRate_ReflectsCriticalRouteStatus()
    {
        _metricsService.Setup(m => m.GetCriticalRouteMetrics())
            .Returns([new RouteMetricsResponse("POST /api/quests/{id}/complete", DomainHealthStatus.Critical, 1200, 1800, 900, 18.5, 42)]);

        var result = await CreateHandler().Handle(new GetPerformanceOverviewQuery(null, null, null), CancellationToken.None);

        result.OverallStatus.Should().Be(DomainHealthStatus.Critical, "rota crítica com erro elevado deve refletir no agregado geral");
        result.CriticalRoutes.Should().ContainSingle(r => r.Status == DomainHealthStatus.Critical && r.ErrorRatePercent == 18.5);
    }

    [Fact]
    public async Task Handle_WhenRouteIsSlow_ReflectsAttentionStatus()
    {
        _metricsService.Setup(m => m.GetCriticalRouteMetrics())
            .Returns([new RouteMetricsResponse("GET /api/progression/me", DomainHealthStatus.Attention, 450, 800, 300, 1.2, 12)]);

        var result = await CreateHandler().Handle(new GetPerformanceOverviewQuery(null, null, null), CancellationToken.None);

        result.OverallStatus.Should().Be(DomainHealthStatus.Attention);
        result.CriticalRoutes.Should().ContainSingle(r => r.Status == DomainHealthStatus.Attention && r.P95Ms == 450);
    }

    [Fact]
    public async Task Handle_CacheHitHighOrLow_StillReturnsNoData_BecauseNoRealCounterExists()
    {
        // RN-002: mesmo que um "cenário de hit alto/baixo" seja simulado pelo QA, esta implementação
        // não inventa números — sem fonte real de contador, o resultado é sempre NoData.
        var result = await CreateHandler().Handle(new GetPerformanceOverviewQuery(null, null, null), CancellationToken.None);

        result.CacheDomains.Should().OnlyContain(c =>
            c.Status == DomainHealthStatus.NoData && c.Hits == null && c.Misses == null && c.HitRatePercent == null);
    }

    [Fact]
    public async Task Handle_WhenRedisUnavailable_OverallStatusIsCritical()
    {
        // RN-004: banco ou Redis indisponível deixa o painel crítico.
        _metricsService.Setup(m => m.GetRedisHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RedisHealthResponse(DomainHealthStatus.Critical, null, false));

        var result = await CreateHandler().Handle(new GetPerformanceOverviewQuery(null, null, null), CancellationToken.None);

        result.OverallStatus.Should().Be(DomainHealthStatus.Critical);
        result.Redis.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenDatabaseHasHighLatency_OverallStatusReflectsCriticalOrAttention()
    {
        // RN-004: banco com latência alta o suficiente para ser Critical deve derrubar o painel inteiro.
        _metricsService.Setup(m => m.GetDatabaseHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DatabaseHealthResponse(DomainHealthStatus.Critical, 750.0));

        var result = await CreateHandler().Handle(new GetPerformanceOverviewQuery(null, null, null), CancellationToken.None);

        result.OverallStatus.Should().Be(DomainHealthStatus.Critical);
        result.Database.LatencyMs.Should().Be(750.0);
    }

    [Fact]
    public async Task Handle_WhenDatabaseIsInAttention_OverallStatusIsAttention_WhenNothingElseIsCritical()
    {
        _metricsService.Setup(m => m.GetDatabaseHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DatabaseHealthResponse(DomainHealthStatus.Attention, 220.0));

        var result = await CreateHandler().Handle(new GetPerformanceOverviewQuery(null, null, null), CancellationToken.None);

        result.OverallStatus.Should().Be(DomainHealthStatus.Attention);
    }

    [Fact]
    public async Task Handle_UsesProvidedEnvironment_OrDefaultsToProd()
    {
        var resultWithEnv = await CreateHandler().Handle(new GetPerformanceOverviewQuery("staging", null, null), CancellationToken.None);
        var resultWithoutEnv = await CreateHandler().Handle(new GetPerformanceOverviewQuery(null, null, null), CancellationToken.None);

        resultWithEnv.Environment.Should().Be("staging");
        resultWithoutEnv.Environment.Should().Be("prod");
    }
}
