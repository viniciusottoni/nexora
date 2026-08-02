using Awaken.Infrastructure.Persistence;
using Awaken.Infrastructure.Services;
using Awaken.Shared.Admin;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using StackExchange.Redis;

namespace Awaken.UnitTests.Admin.Performance;

/// <summary>
/// US-220 — testes do serviço de coleta de performance.
///
/// CA: banco saudável (CanConnectAsync true) reporta status condizente com a latência medida.
/// CA: banco indisponível (CanConnectAsync false) marca status Critical (RN-004).
/// CA: Redis desconectado marca status Critical (RN-004) sem tentar PingAsync.
/// CA: Redis conectado mas PingAsync lança RedisException marca status Critical (RN-004).
/// CA: hit/miss de cache por domínio retorna NoData honesto — não existe fonte real (RN-002).
/// CA: métricas de rota crítica retornam NoData honesto — não existe fonte de APM (RN-001/RN-002).
/// </summary>
public class PerformanceMetricsServiceTests : IDisposable
{
    private readonly AwakenDbContext _healthyContext;
    private readonly Mock<IConnectionMultiplexer> _multiplexer = new();
    private readonly Mock<IDatabase> _redisDb = new();

    public PerformanceMetricsServiceTests()
    {
        var options = new DbContextOptionsBuilder<AwakenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dateTimeService = new Mock<Awaken.Application.Common.Interfaces.IDateTimeService>();
        dateTimeService.Setup(d => d.UtcNow).Returns(new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc));
        _healthyContext = new AwakenDbContext(options, dateTimeService.Object);

        _multiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_redisDb.Object);
    }

    public void Dispose() => _healthyContext.Dispose();

    private PerformanceMetricsService CreateService(AwakenDbContext? context = null) =>
        new(context ?? _healthyContext, _multiplexer.Object);

    [Fact]
    public async Task GetDatabaseHealthAsync_WhenCanConnect_ReturnsNonCriticalStatusWithLatency()
    {
        // InMemory provider's CanConnectAsync always returns true. Warm-up call absorbs first-hit
        // JIT/allocation overhead so the measured latency below reflects steady-state timing,
        // avoiding flakiness on slow CI runners while still exercising the real Stopwatch path.
        await _healthyContext.Database.CanConnectAsync();

        var result = await CreateService().GetDatabaseHealthAsync();

        result.Status.Should().NotBe(DomainHealthStatus.Critical, "CanConnectAsync succeeded, so it must not be reported as unavailable (RN-004)");
        result.LatencyMs.Should().NotBeNull();
        result.LatencyMs!.Value.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetDatabaseHealthAsync_WhenCannotConnect_ReturnsCritical()
    {
        // RN-004: banco indisponível deixa o painel crítico.
        await using var disposedContext = CreateDisposedContext();

        var result = await CreateService(disposedContext).GetDatabaseHealthAsync();

        result.Status.Should().Be(DomainHealthStatus.Critical, "RN-004: banco indisponível deve ser crítico");
        result.LatencyMs.Should().BeNull();
    }

    [Fact]
    public async Task GetRedisHealthAsync_WhenNotConnected_ReturnsCriticalWithoutPinging()
    {
        _multiplexer.SetupGet(m => m.IsConnected).Returns(false);

        var result = await CreateService().GetRedisHealthAsync();

        result.Status.Should().Be(DomainHealthStatus.Critical, "RN-004: Redis indisponível deve ser crítico");
        result.IsConnected.Should().BeFalse();
        result.LatencyMs.Should().BeNull();
        _redisDb.Verify(d => d.PingAsync(It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task GetRedisHealthAsync_WhenPingThrowsRedisException_ReturnsCritical()
    {
        _multiplexer.SetupGet(m => m.IsConnected).Returns(true);
        _redisDb.Setup(d => d.PingAsync(It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "boom"));

        var result = await CreateService().GetRedisHealthAsync();

        result.Status.Should().Be(DomainHealthStatus.Critical, "RN-004: falha de ping deve ser crítica");
        result.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task GetRedisHealthAsync_WhenPingSucceedsFast_ReturnsHealthy()
    {
        _multiplexer.SetupGet(m => m.IsConnected).Returns(true);
        _redisDb.Setup(d => d.PingAsync(It.IsAny<CommandFlags>())).ReturnsAsync(TimeSpan.FromMilliseconds(5));

        var result = await CreateService().GetRedisHealthAsync();

        result.Status.Should().Be(DomainHealthStatus.Healthy);
        result.IsConnected.Should().BeTrue();
        result.LatencyMs.Should().NotBeNull();
    }

    [Fact]
    public void GetCacheDomainMetrics_ReturnsNoData_BecauseNoRealHitMissCounterExistsYet()
    {
        // RN-002: cache sem métricas deve aparecer como sem dados, nunca saudável.
        var result = CreateService().GetCacheDomainMetrics();

        result.Should().NotBeEmpty();
        result.Should().OnlyContain(c => c.Status == DomainHealthStatus.NoData);
        result.Should().OnlyContain(c => c.Hits == null && c.Misses == null && c.HitRatePercent == null);
    }

    [Fact]
    public void GetCriticalRouteMetrics_ReturnsNoData_BecauseNoAggregatedMetricsSourceExistsYet()
    {
        // RN-001/RN-002: sem fonte de métricas agregadas por rota, retorna no_data — nunca inventa números.
        var result = CreateService().GetCriticalRouteMetrics();

        result.Should().NotBeEmpty();
        result.Should().OnlyContain(r => r.Status == DomainHealthStatus.NoData);
        result.Should().OnlyContain(r => r.P95Ms == null && r.P99Ms == null && r.ErrorRatePercent == null && r.RequestsPerSecond == null);
    }

    [Fact]
    public void GetSlowEndpoints_ReturnsEmptyList_BecauseNoAggregatedMetricsSourceExistsYet()
    {
        var result = CreateService().GetSlowEndpoints();

        result.Should().BeEmpty();
    }

    private static AwakenDbContext CreateDisposedContext()
    {
        var options = new DbContextOptionsBuilder<AwakenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dateTimeService = new Mock<Awaken.Application.Common.Interfaces.IDateTimeService>();
        dateTimeService.Setup(d => d.UtcNow).Returns(DateTime.UtcNow);
        var context = new AwakenDbContext(options, dateTimeService.Object);
        context.Dispose();
        return context;
    }
}
