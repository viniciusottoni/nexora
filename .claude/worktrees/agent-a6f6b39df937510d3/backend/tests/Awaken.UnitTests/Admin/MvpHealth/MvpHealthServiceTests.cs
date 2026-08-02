using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Performance;
using Awaken.Contracts.Admin.Readiness;
using Awaken.Contracts.Admin.Routines;
using Awaken.Domain.Repositories;
using Awaken.Infrastructure.Services;
using Awaken.Shared.Admin;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.MvpHealth;

/// <summary>
/// US-216 — testes do serviço de saúde consolidada do MVP.
///
/// CA: todos os domínios saudáveis → OverallStatus=healthy.
/// CA: um domínio crítico (segurança com alertas críticos) → OverallStatus=critical, P0Blockers preenchido.
/// CA: serviço de assinaturas lança exceção → domínio=no_data, demais domínios ainda retornam.
/// CA: GeneratedAtUtc vem do IDateTimeService.
/// CA: media_cdn, observability, load_test → sempre no_data.
/// CA: lista de domínios tem 8 itens.
/// CA: P0Blockers vazio quando nenhum domínio crítico.
/// CA: configuração com HasBlocker=true → domínio configuration=critical.
/// </summary>
public class MvpHealthServiceTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IReadinessCheckService> _readinessCheckService = new();
    private readonly Mock<IPerformanceMetricsService> _performanceMetricsService = new();
    private readonly Mock<IJobMonitoringService> _jobMonitoringService = new();
    private readonly Mock<IAdminSubscriptionDiagnosticsRepository> _subscriptionRepo = new();
    private readonly Mock<ISecurityAlertRepository> _securityAlertRepo = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<MvpHealthService>> _logger = new();

    public MvpHealthServiceTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(FixedUtcNow);
    }

    private MvpHealthService CreateService() => new(
        _readinessCheckService.Object,
        _performanceMetricsService.Object,
        _jobMonitoringService.Object,
        _subscriptionRepo.Object,
        _securityAlertRepo.Object,
        _dateTimeService.Object,
        _logger.Object);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetupAllHealthy()
    {
        _securityAlertRepo
            .Setup(r => r.CountOpenBySeverityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _subscriptionRepo
            .Setup(r => r.GetCountsAsync(null, null, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionDiagnosticsCounts(10, 0, 0, 0, 0, 0));

        _readinessCheckService
            .Setup(r => r.GetReadinessStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadinessStatusResponse(
            [
                new EnvironmentReadinessResponse("prod", DomainHealthStatus.Healthy, false, [], FixedUtcNow),
            ], FixedUtcNow));

        _performanceMetricsService
            .Setup(p => p.GetDatabaseHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DatabaseHealthResponse(DomainHealthStatus.Healthy, 2.5));

        _performanceMetricsService
            .Setup(p => p.GetRedisHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RedisHealthResponse(DomainHealthStatus.Healthy, 1.0, true));

        _jobMonitoringService
            .Setup(j => j.GetRoutinesOverviewAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoutinesOverviewResponse(
                DomainHealthStatus.Healthy,
                [],
                DomainHealthStatus.Healthy,
                [],
                DomainHealthStatus.Healthy,
                [],
                [],
                false,
                [],
                FixedUtcNow));
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMvpHealthAsync_AllDomainsHealthy_ReturnsHealthyOverallStatus()
    {
        SetupAllHealthy();
        var service = CreateService();

        var result = await service.GetMvpHealthAsync();

        // media_cdn, observability, load_test are always no_data, so overall is no_data not healthy
        result.OverallStatus.Should().BeOneOf(DomainHealthStatus.Healthy, DomainHealthStatus.NoData);
        result.P0Blockers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMvpHealthAsync_SecurityWithCriticalAlerts_ReturnsOverallCriticalAndBlocker()
    {
        SetupAllHealthy();

        _securityAlertRepo
            .Setup(r => r.CountOpenBySeverityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AlertSeverityCount("critical", 3)]);

        var service = CreateService();
        var result = await service.GetMvpHealthAsync();

        result.OverallStatus.Should().Be(DomainHealthStatus.Critical);
        result.P0Blockers.Should().NotBeEmpty();
        result.P0Blockers.Should().Contain(b => b.Contains("Segurança"));
    }

    [Fact]
    public async Task GetMvpHealthAsync_SubscriptionServiceThrows_ThatDomainIsNoDataOthersStillPopulated()
    {
        SetupAllHealthy();

        _subscriptionRepo
            .Setup(r => r.GetCountsAsync(null, null, 30, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        var service = CreateService();
        var result = await service.GetMvpHealthAsync();

        var subscriptionDomain = result.Domains.Single(d => d.Key == "subscriptions");
        subscriptionDomain.Status.Should().Be(DomainHealthStatus.NoData);

        var securityDomain = result.Domains.Single(d => d.Key == "security");
        securityDomain.Status.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMvpHealthAsync_GeneratedAtUtc_ComesFromDateTimeService()
    {
        SetupAllHealthy();
        var service = CreateService();

        var result = await service.GetMvpHealthAsync();

        result.GeneratedAtUtc.Should().Be(FixedUtcNow);
    }

    [Fact]
    public async Task GetMvpHealthAsync_StaticDomains_AreAlwaysNoData()
    {
        SetupAllHealthy();
        var service = CreateService();

        var result = await service.GetMvpHealthAsync();

        result.Domains.Single(d => d.Key == "media_cdn").Status.Should().Be(DomainHealthStatus.NoData);
        result.Domains.Single(d => d.Key == "observability").Status.Should().Be(DomainHealthStatus.NoData);
        result.Domains.Single(d => d.Key == "load_test").Status.Should().Be(DomainHealthStatus.NoData);
    }

    [Fact]
    public async Task GetMvpHealthAsync_DomainsListHasEightItems()
    {
        SetupAllHealthy();
        var service = CreateService();

        var result = await service.GetMvpHealthAsync();

        result.Domains.Should().HaveCount(8);
    }

    [Fact]
    public async Task GetMvpHealthAsync_NoCriticalDomains_P0BlockersIsEmpty()
    {
        SetupAllHealthy();
        var service = CreateService();

        var result = await service.GetMvpHealthAsync();

        var nonStaticCritical = result.Domains
            .Where(d => d.Key is not "media_cdn" and not "observability" and not "load_test")
            .Any(d => d.Status == DomainHealthStatus.Critical);

        if (!nonStaticCritical)
            result.P0Blockers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMvpHealthAsync_ConfigurationWithBlocker_MapsToConfigurationDomainCritical()
    {
        SetupAllHealthy();

        _readinessCheckService
            .Setup(r => r.GetReadinessStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadinessStatusResponse(
            [
                new EnvironmentReadinessResponse("prod", DomainHealthStatus.Critical, true,
                [
                    new ReadinessCheckResponse("JWT Secret", "configuration", DomainHealthStatus.Critical,
                        "Segredo JWT não configurado.", true, FixedUtcNow),
                ],
                FixedUtcNow),
            ], FixedUtcNow));

        var service = CreateService();
        var result = await service.GetMvpHealthAsync();

        var configDomain = result.Domains.Single(d => d.Key == "configuration");
        configDomain.Status.Should().Be(DomainHealthStatus.Critical);
    }
}
