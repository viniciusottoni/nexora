using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Awaken.Contracts.Admin.Timeline;
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
/// US-224: testes de integração do endpoint de timeline operacional no admin site.
///
/// O endpoint agrega entradas de segurança e auditoria em ordem cronológica descrescente.
/// Em ambiente de teste não há eventos reais; o endpoint deve responder 200 com uma lista
/// vazia e um resumo de impacto zerado — nunca 500.
/// </summary>
public class AdminTimelineEndpointTests : IAsyncLifetime
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
    public async Task GetTimeline_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/admin/timeline");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTimeline_WhenAdmin_Returns200()
    {
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/timeline");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTimeline_ResponseHasEntriesList()
    {
        // O corpo da resposta deve conter a propriedade "entries" como array (possivelmente vazio em
        // ambiente de teste sem eventos reais).
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/timeline");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OperationalTimelineResponse>();
        body.Should().NotBeNull();
        body!.Entries.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTimeline_ResponseHasImpactSummary()
    {
        // O corpo da resposta deve conter a propriedade "impact" com "estimatedUsersAffected".
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/timeline");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OperationalTimelineResponse>();
        body.Should().NotBeNull();
        body!.Impact.Should().NotBeNull();
        body.Impact.EstimatedUsersAffected.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetTimeline_WithFilters_Returns200()
    {
        // Filtros de severidade e ambiente devem ser aceitos sem erro; o servidor ignora valores
        // desconhecidos de forma graciosa em vez de retornar 400/500.
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/timeline?severity=critical&environment=prod");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
