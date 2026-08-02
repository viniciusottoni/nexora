using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Awaken.Contracts.Admin.Readiness;
using Awaken.Infrastructure.Persistence;
using Awaken.Shared.Admin;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

/// <summary>
/// US-218: testes de integração do endpoint de readiness de configuração/build/CI no admin site.
/// RN-001: a resposta nunca deve carregar valores reais de configuração — apenas presença/status.
/// </summary>
public class AdminReadinessEndpointTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("awaken")
        .WithUsername("awaken")
        .WithPassword("awaken_test_password")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PostgreSQL"] = _postgres.GetConnectionString(),
                });
            });
        });

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        await dbContext.Database.MigrateAsync();

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string GenerateAdminToken()
    {
        var config = _factory.Services.GetRequiredService<IConfiguration>();
        var section = config.GetSection("AdminJwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "AdminSite"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: section["Issuer"],
            audience: section["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetReadiness_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/admin/readiness");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetReadiness_WithoutAdminRole_Returns403()
    {
        var config = _factory.Services.GetRequiredService<IConfiguration>();
        var section = config.GetSection("AdminJwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()) };
        var token = new JwtSecurityToken(
            issuer: section["Issuer"], audience: section["Audience"], claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30), signingCredentials: creds);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var response = await _client.GetAsync("/api/admin/readiness");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetReadiness_WhenAdmin_Returns200WithAllMonitoredEnvironments()
    {
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/readiness");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ReadinessStatusResponse>();
        body.Should().NotBeNull();
        body!.Environments.Select(e => e.Environment).Should().BeEquivalentTo(["dev", "staging", "prod"]);
    }

    [Fact]
    public async Task GetReadiness_EachEnvironment_HasConfigurationBuildAndCiChecks()
    {
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/readiness");
        var body = await response.Content.ReadFromJsonAsync<ReadinessStatusResponse>();

        foreach (var env in body!.Environments)
        {
            env.Checks.Should().Contain(c => c.Category == "configuration");
            env.Checks.Should().Contain(c => c.Category == "build");
            env.Checks.Should().Contain(c => c.Category == "ci");
            env.Checks.Should().OnlyContain(c => !string.IsNullOrWhiteSpace(c.Description));
        }
    }

    [Fact]
    public async Task GetReadiness_BuildAndCiChecks_ReportNoData_NotSimulatedSuccess()
    {
        // RN-003/RN-004: sem fonte real de build mobile/CI no MVP, deve ser NoData honesto,
        // nunca Healthy simulado.
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/readiness");
        var body = await response.Content.ReadFromJsonAsync<ReadinessStatusResponse>();

        foreach (var env in body!.Environments)
        {
            env.Checks.Where(c => c.Category is "build" or "ci")
                .Should().OnlyContain(c => c.Status == DomainHealthStatus.NoData);
        }
    }

    [Fact]
    public async Task GetReadiness_ResponseBody_NeverContainsConfiguredConnectionStringValue()
    {
        // RN-001: a tela/endpoint nunca deve expor valores sensíveis de configuração.
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/readiness");
        var raw = await response.Content.ReadAsStringAsync();

        raw.Should().NotContain("awaken_test_password");
        raw.Should().NotContain(_postgres.GetConnectionString());
    }

    [Fact]
    public async Task GetReadiness_ProductionEnvironment_AggregatesOverallStatus()
    {
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/readiness");
        var body = await response.Content.ReadFromJsonAsync<ReadinessStatusResponse>();

        var prod = body!.Environments.Single(e => e.Environment == "prod");
        prod.OverallStatus.Should().BeOneOf(
            DomainHealthStatus.Healthy, DomainHealthStatus.Attention,
            DomainHealthStatus.Critical, DomainHealthStatus.NoData);
    }
}
