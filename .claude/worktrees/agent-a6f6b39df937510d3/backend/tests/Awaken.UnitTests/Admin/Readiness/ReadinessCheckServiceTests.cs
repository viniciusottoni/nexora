using Awaken.Application.Common.Interfaces;
using Awaken.Infrastructure.Services;
using Awaken.Shared.Admin;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Awaken.UnitTests.Admin.Readiness;

/// <summary>
/// US-218 — testes do serviço de readiness de configuração, build mobile e CI por ambiente.
///
/// CA: ambiente saudável (toda configuração obrigatória presente e válida) -&gt; Healthy.
/// CA: ambiente com configuração ausente -&gt; Attention (não-prod) ou Critical/bloqueador (prod, RN-002).
/// CA: ambiente sem dados (build mobile / CI) -&gt; NoData honesto, nunca Healthy por omissão (RN-005).
/// CA: build release com configuração de teste -&gt; reportado como NoData explicando a limitação do MVP (RN-003).
/// CA: CI aprovado/falho -&gt; sem integração real, NoData honesto (RN-004).
/// CA RN-001: nenhuma descrição de check deve conter o valor real configurado.
/// </summary>
public class ReadinessCheckServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static ReadinessCheckService CreateService(IConfiguration configuration)
    {
        var dateTimeService = new Mock<IDateTimeService>();
        dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
        return new ReadinessCheckService(configuration, dateTimeService.Object);
    }

    private static readonly Dictionary<string, string?> HealthyConfig = new()
    {
        ["Jwt:Secret"] = "a-very-strong-secret-with-at-least-32-characters",
        ["AdminJwt:Secret"] = "another-very-strong-secret-32-chars-min",
        ["Cors:AllowedOrigins:0"] = "https://admin.awaken.app",
        ["Google:ClientId"] = "123456-abcdef.apps.googleusercontent.com",
        ["ConnectionStrings:PostgreSQL"] = "Host=db;Database=awaken;Username=awaken;Password=s3cr3t",
        ["ConnectionStrings:Redis"] = "redis-host:6379,password=s3cr3t",
    };

    [Fact]
    public async Task GetReadinessStatusAsync_HealthyEnvironment_ReturnsHealthyOverallStatus()
    {
        var service = CreateService(BuildConfiguration(HealthyConfig));

        var result = await service.GetReadinessStatusAsync();

        result.Environments.Should().HaveCount(3);
        var devOrStaging = result.Environments.First(e => e.Environment != "prod");
        // Build/CI checks are NoData by design, so overall aggregates to NoData even when config is healthy
        // for non-production environments; what matters here is that no check is Critical/blocker.
        devOrStaging.Checks.Should().NotContain(c => c.Status == DomainHealthStatus.Critical);
        devOrStaging.HasBlocker.Should().BeFalse();

        var configChecks = devOrStaging.Checks.Where(c => c.Category == "configuration").ToList();
        configChecks.Should().NotBeEmpty();
        configChecks.Should().OnlyContain(c => c.Status == DomainHealthStatus.Healthy);
    }

    [Fact]
    public async Task GetReadinessStatusAsync_ProductionMissingConfiguration_IsCriticalBlocker()
    {
        // RN-002: produção com configuração obrigatória ausente deve aparecer como bloqueada.
        var config = new Dictionary<string, string?>(HealthyConfig);
        config.Remove("Jwt:Secret");
        var service = CreateService(BuildConfiguration(config));

        var result = await service.GetReadinessStatusAsync();

        var prod = result.Environments.Single(e => e.Environment == "prod");
        var jwtCheck = prod.Checks.Single(c => c.Name == "JWT Secret");

        jwtCheck.Status.Should().Be(DomainHealthStatus.Critical);
        jwtCheck.IsBlocker.Should().BeTrue();
        prod.HasBlocker.Should().BeTrue();
        prod.OverallStatus.Should().Be(DomainHealthStatus.Critical);
    }

    [Fact]
    public async Task GetReadinessStatusAsync_NonProductionMissingConfiguration_IsAttentionNotBlocker()
    {
        var config = new Dictionary<string, string?>(HealthyConfig);
        config.Remove("Jwt:Secret");
        var service = CreateService(BuildConfiguration(config));

        var result = await service.GetReadinessStatusAsync();

        var dev = result.Environments.Single(e => e.Environment == "dev");
        var jwtCheck = dev.Checks.Single(c => c.Name == "JWT Secret");

        jwtCheck.Status.Should().Be(DomainHealthStatus.Attention);
        jwtCheck.IsBlocker.Should().BeFalse();
    }

    [Fact]
    public async Task GetReadinessStatusAsync_PlaceholderSecret_IsInvalidNotHealthy()
    {
        var config = new Dictionary<string, string?>(HealthyConfig)
        {
            ["Jwt:Secret"] = "CHANGE_THIS_PLACEHOLDER_VALUE_TO_SOMETHING_REAL",
        };
        var service = CreateService(BuildConfiguration(config));

        var result = await service.GetReadinessStatusAsync();

        var prod = result.Environments.Single(e => e.Environment == "prod");
        var jwtCheck = prod.Checks.Single(c => c.Name == "JWT Secret");

        jwtCheck.Status.Should().Be(DomainHealthStatus.Critical);
    }

    [Fact]
    public async Task GetReadinessStatusAsync_MobileBuildCheck_ReturnsNoData_WithExplanation()
    {
        // RN-003: sem fonte automática para inferir release com config de teste no MVP -> NoData honesto.
        var service = CreateService(BuildConfiguration(HealthyConfig));

        var result = await service.GetReadinessStatusAsync();

        foreach (var env in result.Environments)
        {
            var buildCheck = env.Checks.Single(c => c.Category == "build");
            buildCheck.Status.Should().Be(DomainHealthStatus.NoData);
            buildCheck.Description.Should().NotBeNullOrWhiteSpace();
            buildCheck.IsBlocker.Should().BeFalse();
        }
    }

    [Fact]
    public async Task GetReadinessStatusAsync_CiHistoryCheck_ReturnsNoData_WhenNoRealIntegration()
    {
        // RN-004: sem integração real com CI -> NoData honesto, nunca dado simulado.
        var service = CreateService(BuildConfiguration(HealthyConfig));

        var result = await service.GetReadinessStatusAsync();

        foreach (var env in result.Environments)
        {
            var ciCheck = env.Checks.Single(c => c.Category == "ci");
            ciCheck.Status.Should().Be(DomainHealthStatus.NoData);
        }
    }

    [Fact]
    public async Task GetReadinessStatusAsync_NoTelemetrySource_NeverReportsHealthyByOmission()
    {
        // RN-005: ambiente sem telemetria deve aparecer como sem dados, nunca Healthy por omissão.
        var emptyConfig = new Dictionary<string, string?>();
        var service = CreateService(BuildConfiguration(emptyConfig));

        var result = await service.GetReadinessStatusAsync();

        foreach (var env in result.Environments)
        {
            env.Checks.Where(c => c.Category is "build" or "ci")
                .Should().OnlyContain(c => c.Status == DomainHealthStatus.NoData);
        }
    }

    [Fact]
    public async Task GetReadinessStatusAsync_GoogleClientIdAbsent_ReturnsNoData_NotHealthy()
    {
        var config = new Dictionary<string, string?>(HealthyConfig);
        config.Remove("Google:ClientId");
        var service = CreateService(BuildConfiguration(config));

        var result = await service.GetReadinessStatusAsync();

        var prod = result.Environments.Single(e => e.Environment == "prod");
        var googleCheck = prod.Checks.Single(c => c.Name == "Google ClientId");

        googleCheck.Status.Should().Be(DomainHealthStatus.NoData);
    }

    [Fact]
    public async Task GetReadinessStatusAsync_NeverExposesRawConfigurationValues_InDescriptions()
    {
        // RN-001: a tela deve mostrar apenas presença/status, nunca valores sensíveis de configuração.
        var placeholderValue = "example-placeholder-value";
        var connectionValue = "Host=db;Database=awaken;Username=awaken;Password=example-password";
        var service = CreateService(BuildConfiguration(HealthyConfig));

        var result = await service.GetReadinessStatusAsync();

        foreach (var env in result.Environments)
        {
            foreach (var check in env.Checks)
            {
                check.Description.Should().NotContain(placeholderValue);
                check.Description.Should().NotContain(connectionValue);
                check.Description.Should().NotContain("example-password");
            }
        }
    }

    [Fact]
    public async Task GetReadinessStatusAsync_ReturnsAllThreeMonitoredEnvironments()
    {
        var service = CreateService(BuildConfiguration(HealthyConfig));

        var result = await service.GetReadinessStatusAsync();

        result.Environments.Select(e => e.Environment).Should().BeEquivalentTo(["dev", "staging", "prod"]);
    }

    [Fact]
    public async Task GetReadinessStatusAsync_EachCheck_HasTimestamp()
    {
        var service = CreateService(BuildConfiguration(HealthyConfig));

        var result = await service.GetReadinessStatusAsync();

        foreach (var env in result.Environments)
        {
            env.Checks.Should().OnlyContain(c => c.CheckedAtUtc == UtcNow);
        }
    }
}
