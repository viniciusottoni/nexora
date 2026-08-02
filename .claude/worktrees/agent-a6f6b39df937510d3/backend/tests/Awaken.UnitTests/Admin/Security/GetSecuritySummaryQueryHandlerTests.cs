using Awaken.Application.Admin.Security.Queries.GetSecuritySummary;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.Security;

/// <summary>
/// US-219: resumo preventivo agregado de segurança.
/// RN-003: aumento repentino de falhas de login/tokens inválidos/limite atingido/autorização
/// negada deve ser sinalizado via IsSpike para destaque visual no frontend.
/// </summary>
public class GetSecuritySummaryQueryHandlerTests
{
    private readonly Mock<ISecurityAlertRepository> _securityAlertRepository = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();

    private static readonly DateTime UtcNow = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);

    public GetSecuritySummaryQueryHandlerTests()
    {
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
    }

    private GetSecuritySummaryQueryHandler CreateHandler() => new(_securityAlertRepository.Object, _dateTimeService.Object);

    private void SetupEmptyDefaults()
    {
        _securityAlertRepository
            .Setup(r => r.CountOpenBySeverityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _securityAlertRepository
            .Setup(r => r.CountByEnvironmentAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _securityAlertRepository
            .Setup(r => r.GetHourlyTimeSeriesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _securityAlertRepository
            .Setup(r => r.GetTopMaskedOriginsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _securityAlertRepository
            .Setup(r => r.CountByAffectedUserAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _securityAlertRepository
            .Setup(r => r.CountRateLimitByEndpointAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _securityAlertRepository
            .Setup(r => r.CountRbacDeniedByResourceAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _securityAlertRepository
            .Setup(r => r.GetAverageMinutesToAnalysisAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((double?)null);
    }

    [Fact]
    public async Task Handle_LoginFailureSpike_MarksTrendAsSpike()
    {
        // Pico de falha de login: 20 no período atual vs. 5 no período anterior (=300% delta).
        SetupEmptyDefaults();
        _securityAlertRepository
            .Setup(r => r.CountByAlertTypeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AlertTypeCount("brute_force", 20)]);
        _securityAlertRepository
            .Setup(r => r.CountByAlertTypeInWindowAsync("brute_force", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var result = await CreateHandler().Handle(new GetSecuritySummaryQuery(null, null), CancellationToken.None);

        var trend = result.AlertTypeTrends.Should().ContainSingle(t => t.AlertType == "brute_force").Subject;
        trend.Count.Should().Be(20);
        trend.PreviousCount.Should().Be(5);
        trend.DeltaPercent.Should().Be(300.0);
        trend.IsSpike.Should().BeTrue("RN-003: aumento repentino de falhas de login deve ser destacado");
    }

    [Fact]
    public async Task Handle_ManyRateLimitHits_ReturnsRateLimitByEndpointGrouping()
    {
        SetupEmptyDefaults();
        _securityAlertRepository
            .Setup(r => r.CountByAlertTypeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AlertTypeCount("rate_limit_hit", 50)]);
        _securityAlertRepository
            .Setup(r => r.CountByAlertTypeInWindowAsync("rate_limit_hit", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);
        _securityAlertRepository
            .Setup(r => r.CountRateLimitByEndpointAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AlertEndpointCount("quest_complete_endpoint", 30), new AlertEndpointCount("login_endpoint", 20)]);

        var result = await CreateHandler().Handle(new GetSecuritySummaryQuery(null, null), CancellationToken.None);

        result.RateLimitHitsByEndpoint.Should().HaveCount(2);
        result.RateLimitHitsByEndpoint.Should().Contain(e => e.Endpoint == "quest_complete_endpoint" && e.Count == 30);

        var trend = result.AlertTypeTrends.Should().ContainSingle(t => t.AlertType == "rate_limit_hit").Subject;
        trend.IsSpike.Should().BeTrue("400% de aumento em rate_limit_hit deve gerar destaque (RN-003)");
    }

    [Fact]
    public async Task Handle_RbacDenied_ReturnsDeniedByResourceGrouping()
    {
        SetupEmptyDefaults();
        _securityAlertRepository
            .Setup(r => r.CountByAlertTypeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AlertTypeCount("rbac_denied", 8)]);
        _securityAlertRepository
            .Setup(r => r.CountByAlertTypeInWindowAsync("rbac_denied", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(8);
        _securityAlertRepository
            .Setup(r => r.CountRbacDeniedByResourceAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AlertEndpointCount("admin_users_export", 8)]);

        var result = await CreateHandler().Handle(new GetSecuritySummaryQuery(null, null), CancellationToken.None);

        result.RbacDeniedByResource.Should().ContainSingle(e => e.Endpoint == "admin_users_export" && e.Count == 8);
    }

    [Fact]
    public async Task Handle_CriticalAlert_IsReflectedInOpenAlertsBySeverity()
    {
        SetupEmptyDefaults();
        _securityAlertRepository
            .Setup(r => r.CountByAlertTypeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _securityAlertRepository
            .Setup(r => r.CountOpenBySeverityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AlertSeverityCount("critical", 3), new AlertSeverityCount("low", 12)]);

        var result = await CreateHandler().Handle(new GetSecuritySummaryQuery(null, null), CancellationToken.None);

        result.OpenAlertsBySeverity.Should().Contain(s => s.Severity == "critical" && s.Count == 3);
    }

    [Fact]
    public async Task Handle_NoSpikeWatchedType_StableVolumeDoesNotFlagSpike()
    {
        SetupEmptyDefaults();
        _securityAlertRepository
            .Setup(r => r.CountByAlertTypeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AlertTypeCount("suspicious_traffic", 10)]);
        _securityAlertRepository
            .Setup(r => r.CountByAlertTypeInWindowAsync("suspicious_traffic", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        var result = await CreateHandler().Handle(new GetSecuritySummaryQuery(null, null), CancellationToken.None);

        var trend = result.AlertTypeTrends.Should().ContainSingle().Subject;
        trend.DeltaPercent.Should().Be(0.0);
        trend.IsSpike.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DefaultWindow_Is24Hours_WhenNoRangeProvided()
    {
        SetupEmptyDefaults();
        _securityAlertRepository
            .Setup(r => r.CountByAlertTypeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetSecuritySummaryQuery(null, null), CancellationToken.None);

        result.ToUtc.Should().Be(UtcNow);
        (result.ToUtc - result.FromUtc).Should().Be(TimeSpan.FromHours(24));
    }

    [Fact]
    public async Task Handle_AverageMinutesToAnalysis_IsPropagatedFromRepository()
    {
        SetupEmptyDefaults();
        _securityAlertRepository
            .Setup(r => r.CountByAlertTypeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _securityAlertRepository
            .Setup(r => r.GetAverageMinutesToAnalysisAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42.5);

        var result = await CreateHandler().Handle(new GetSecuritySummaryQuery(null, null), CancellationToken.None);

        result.AverageMinutesToAnalysis.Should().Be(42.5);
    }
}
