using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Awaken.Contracts.Admin.Routines;
using Awaken.Infrastructure.Persistence;
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
/// US-221: testes de integração do monitoramento de rotinas, workers e filas no admin site.
///
/// Importante: o ambiente de teste não sobe um Awaken.Worker (AddHangfireServer não é chamado pela
/// API), então não há servidores Hangfire ativos durante este teste. O endpoint deve continuar
/// respondendo 200 de forma graciosa nesse cenário — workers=0 e WorkersStatus=Critical (RN-003),
/// em vez de lançar exceção/500.
/// </summary>
public class AdminRoutinesEndpointTests : IAsyncLifetime
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
    public async Task GetRoutinesOverview_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/admin/routines");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRoutinesOverview_WithoutAdminRole_Returns403()
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
        var response = await _client.GetAsync("/api/admin/routines");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetRoutinesOverview_WhenAdmin_Returns200WithCorrectSchema()
    {
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/routines");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RoutinesOverviewResponse>();
        body.Should().NotBeNull();
        body!.Workers.Should().NotBeNull();
        body.Routines.Should().NotBeNull();
        body.Queues.Should().NotBeNull();
        body.RecentExecutions.Should().NotBeNull();
        body.OperationalUpdates.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRoutinesOverview_WhenNoHangfireServerActive_ReturnsCriticalWorkersGracefully()
    {
        // Sem Awaken.Worker rodando neste ambiente de teste, Servers() deve retornar vazio.
        // RN-003: zero servidores ativos -> WorkersStatus crítico, sem lançar exceção/500.
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/routines");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RoutinesOverviewResponse>();
        body!.Workers.Should().BeEmpty();
        body.WorkersStatus.Should().Be("critical");
    }

    [Fact]
    public async Task GetRoutinesOverview_WhenNoOperationalUpdateSource_ReturnsEmptyListHonestly()
    {
        // RN-004: sem tabela dedicada neste MVP, a lista deve vir vazia com a flag indicando ausência
        // de fonte, nunca dado inventado.
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/routines");

        var body = await response.Content.ReadFromJsonAsync<RoutinesOverviewResponse>();
        body!.OperationalUpdatesAvailable.Should().BeFalse();
        body.OperationalUpdates.Should().BeEmpty();
    }
}
