using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Awaken.Contracts.Admin.MvpHealth;
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
/// US-216: testes de integração do endpoint de saúde do MVP no admin site.
/// ADR-015: a resposta nunca deve carregar credenciais, tokens ou payloads sensíveis.
/// </summary>
public class AdminMvpHealthEndpointTests : IAsyncLifetime
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
    public async Task GetMvpHealth_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/admin/mvp-health");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMvpHealth_WithAdminToken_Returns200()
    {
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/mvp-health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMvpHealth_ResponseBody_HasOverallStatusField()
    {
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/mvp-health");
        var body = await response.Content.ReadFromJsonAsync<MvpHealthStatusResponse>();

        body.Should().NotBeNull();
        body!.OverallStatus.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetMvpHealth_ResponseBody_HasNonEmptyDomainsArray()
    {
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/mvp-health");
        var body = await response.Content.ReadFromJsonAsync<MvpHealthStatusResponse>();

        body.Should().NotBeNull();
        body!.Domains.Should().NotBeNull();
        body.Domains.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMvpHealth_ResponseBody_HasP0BlockersField()
    {
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/mvp-health");
        var body = await response.Content.ReadFromJsonAsync<MvpHealthStatusResponse>();

        body.Should().NotBeNull();
        body!.P0Blockers.Should().NotBeNull();
    }
}
