using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Awaken.Contracts.Admin.Performance;
using Awaken.Infrastructure.Persistence;
using Awaken.Shared.Admin;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Awaken.IntegrationTests;

/// <summary>
/// US-220: testes de integração do painel de performance (banco, Redis, caches, rotas críticas)
/// no admin site. Usa PostgreSQL real via Testcontainers em todos os cenários.
///
/// RN-002: cache sem métricas reais (sempre o caso hoje) deve aparecer como "no_data".
/// RN-004: banco ou Redis indisponível deve deixar o painel "critical".
/// </summary>
public class AdminPerformanceEndpointTests : IAsyncLifetime
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

        var pgConnectionString = _postgres.GetConnectionString();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            // appsettings.Local.json (loaded inside Program.cs) overrides UseSetting for both
            // PostgreSQL and Redis. Replace both via ConfigureTestServices (runs after all
            // Program.cs registrations) to get: real Testcontainers PG + disconnected Redis stub.
            builder.ConfigureTestServices(services =>
            {
                var pgDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AwakenDbContext>));
                if (pgDescriptor is not null) services.Remove(pgDescriptor);
                services.AddDbContext<AwakenDbContext>(options => options.UseNpgsql(pgConnectionString));

                var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
                if (redisDescriptor is not null) services.Remove(redisDescriptor);
                var redisOptions = ConfigurationOptions.Parse("127.0.0.1:19999,abortConnect=false,connectTimeout=50,syncTimeout=50");
                services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
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

    private void AuthenticateAsAdmin()
    {
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOverview_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/admin/performance");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOverview_WithoutAdminRole_Returns403()
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
        var response = await _client.GetAsync("/api/admin/performance");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOverview_WhenAdmin_Returns200WithFullShape()
    {
        AuthenticateAsAdmin();

        var response = await _client.GetAsync("/api/admin/performance");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PerformanceOverviewResponse>();
        body.Should().NotBeNull();
        body!.Database.Should().NotBeNull();
        body.Redis.Should().NotBeNull();
        body.CacheDomains.Should().NotBeEmpty();
        body.CriticalRoutes.Should().NotBeEmpty();
        body.LastCollectedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOverview_DatabaseIsReachable_ReportsNonCriticalStatusWithLatency()
    {
        // "rota saudável" / "banco com latência alta" baseline: a Testcontainers PostgreSQL real
        // está acessível, então o status de banco nunca deve ser Critical aqui.
        AuthenticateAsAdmin();

        var response = await _client.GetAsync("/api/admin/performance");
        var body = await response.Content.ReadFromJsonAsync<PerformanceOverviewResponse>();

        body!.Database.Status.Should().NotBe(DomainHealthStatus.Critical);
        body.Database.LatencyMs.Should().NotBeNull();
        body.Database.LatencyMs!.Value.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetOverview_RedisUnavailable_MarksRedisAndOverallStatusAsCritical()
    {
        // RN-004: Redis indisponível (placeholder de appsettings, nunca conectado) deixa
        // o painel crítico — banco está saudável (Postgres real via Testcontainers).
        AuthenticateAsAdmin();

        var response = await _client.GetAsync("/api/admin/performance");
        var body = await response.Content.ReadFromJsonAsync<PerformanceOverviewResponse>();

        body!.Redis.Status.Should().Be(DomainHealthStatus.Critical);
        body.Redis.IsConnected.Should().BeFalse();
        body.OverallStatus.Should().Be(DomainHealthStatus.Critical);
    }

    [Fact]
    public async Task GetOverview_CacheDomains_AllReportNoData_BecauseNoRealHitMissCounterExistsYet()
    {
        // RN-002: cache sem métricas deve aparecer como sem dados, nunca saudável.
        AuthenticateAsAdmin();

        var response = await _client.GetAsync("/api/admin/performance");
        var body = await response.Content.ReadFromJsonAsync<PerformanceOverviewResponse>();

        body!.CacheDomains.Should().OnlyContain(c => c.Status == DomainHealthStatus.NoData);
        body.CacheDomains.Should().OnlyContain(c => c.Hits == null && c.Misses == null && c.HitRatePercent == null);
    }

    [Fact]
    public async Task GetOverview_CriticalRoutes_AllReportNoData_BecauseNoApmSourceExistsYet()
    {
        // RN-001/RN-002: sem fonte de métricas agregadas por rota, retorna no_data — nunca inventa.
        AuthenticateAsAdmin();

        var response = await _client.GetAsync("/api/admin/performance");
        var body = await response.Content.ReadFromJsonAsync<PerformanceOverviewResponse>();

        body!.CriticalRoutes.Should().OnlyContain(r => r.Status == DomainHealthStatus.NoData);
        body.SlowEndpoints.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOverview_ResponseBody_NeverContainsConnectionStringSecrets()
    {
        // RN-005: dados agregados e não expõe payload de usuário/segredos de infraestrutura.
        AuthenticateAsAdmin();

        var response = await _client.GetAsync("/api/admin/performance");
        var raw = await response.Content.ReadAsStringAsync();

        raw.Should().NotContain("awaken_test_password");
        raw.Should().NotContain(_postgres.GetConnectionString());
    }

    [Fact]
    public async Task GetOverview_WithEnvironmentFilter_EchoesRequestedEnvironment()
    {
        AuthenticateAsAdmin();

        var response = await _client.GetAsync("/api/admin/performance?environment=staging");
        var body = await response.Content.ReadFromJsonAsync<PerformanceOverviewResponse>();

        body!.Environment.Should().Be("staging");
    }
}

/// <summary>
/// US-220: cenário isolado com Redis real (Testcontainers) saudável, validando que o painel
/// reporta status não-crítico quando ambas as dependências (Postgres + Redis) estão disponíveis.
/// Em classe separada porque o Redis real precisa ser fornecido como ConnectionStrings:Redis
/// antes do app subir (IConnectionMultiplexer é singleton resolvido na primeira request).
/// </summary>
public class AdminPerformanceEndpointHealthyRedisTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("awaken")
        .WithUsername("awaken")
        .WithPassword("awaken_test_password")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        var pgConnectionString = _postgres.GetConnectionString();
        var redisConnectionString = _redis.GetConnectionString();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                var pgDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AwakenDbContext>));
                if (pgDescriptor is not null) services.Remove(pgDescriptor);
                services.AddDbContext<AwakenDbContext>(options => options.UseNpgsql(pgConnectionString));

                var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
                if (redisDescriptor is not null) services.Remove(redisDescriptor);
                services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
            });
        });

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        await dbContext.Database.MigrateAsync();
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        await redis.GetDatabase().PingAsync();

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
        await _redis.DisposeAsync();
    }

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

    [Fact]
    public async Task GetOverview_WhenPostgresAndRedisAreHealthy_OverallStatusIsNotCritical()
    {
        // "rota saudável": ambas as dependências reais (Testcontainers) disponíveis.
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/performance");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PerformanceOverviewResponse>();

        body!.Database.Status.Should().NotBe(DomainHealthStatus.Critical);
        body.Redis.Status.Should().NotBe(DomainHealthStatus.Critical);
        body.Redis.IsConnected.Should().BeTrue();
        body.OverallStatus.Should().NotBe(DomainHealthStatus.Critical, "RN-004: ambas dependências saudáveis não devem derrubar o painel");
    }
}
