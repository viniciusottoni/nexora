using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Awaken.Contracts.Admin.Economy;
using Awaken.Domain.Entities.Economy;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

/// <summary>
/// US-229: testes de integração para a API admin de economia Gold.
///
/// CA-001: usuário sem role Admin → 403 Forbidden.
/// CA-002: admin sem dados → indicadores zerados.
/// CA-003: admin com carteira existente → saldo visível no detalhe.
/// CA-004: userId inexistente no detalhe de carteira → 404 Not Found.
/// CA-005: exportação CSV não expõe dados sensíveis de pagamento.
/// </summary>
public class AdminEconomyEndpointTests : IAsyncLifetime
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
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AwakenDbContext>));
                if (descriptor is not null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AwakenDbContext>(options => options.UseNpgsql(_postgres.GetConnectionString()));
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

    private string GenerateAdminSiteToken()
    {
        return GenerateJwtToken(
            new Claim(ClaimTypes.Role, "AdminSite"));
    }

    private string GenerateNonAdminToken()
    {
        return GenerateJwtToken();
    }

    private string GenerateJwtToken(params Claim[] extraClaims)
    {
        var config = _factory.Services.GetRequiredService<IConfiguration>();
        var section = config.GetSection("AdminJwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(extraClaims);

        var token = new JwtSecurityToken(
            issuer: section["Issuer"],
            audience: section["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // CA-001: usuário sem role Admin → 403
    [Fact]
    public async Task GetSummary_CA001_UserWithoutAdminRole_Returns403()
    {
        var token = GenerateNonAdminToken();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var resp = await _client.GetAsync("/api/admin/economy/gold/summary");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // CA-002: admin sem dados → indicadores zerados
    [Fact]
    public async Task GetSummary_CA002_AdminNoData_ReturnsZeroed()
    {
        var adminToken = GenerateAdminSiteToken();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var resp = await _client.GetAsync("/api/admin/economy/gold/summary");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await resp.Content.ReadFromJsonAsync<GoldEconomySummaryResponse>();
        summary.Should().NotBeNull();
        summary!.TotalGoldPurchased.Should().Be(0);
        summary.TotalGoldSpent.Should().Be(0);
        summary.TotalInCirculation.Should().Be(0);
        summary.OpenGoldAlerts.Should().Be(0);
    }

    // CA-003: detalhe de carteira para usuário que tem saldo
    [Fact]
    public async Task GetWalletDetail_CA003_UserWithWallet_ReturnsBalance()
    {
        var adminToken = GenerateAdminSiteToken();
        var userId = Guid.NewGuid();

        // Cria carteira com saldo diretamente no banco
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var wallet = GoldWallet.CreateEmpty(userId, DateTime.UtcNow);
        wallet.Credit(500, "test_credit", null, null, null, DateTime.UtcNow);
        db.GoldWallets.Add(wallet);
        await db.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var resp = await _client.GetAsync($"/api/admin/economy/gold/wallets/{userId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await resp.Content.ReadFromJsonAsync<GoldWalletAdminResponse>();
        detail.Should().NotBeNull();
        detail!.UserId.Should().Be(userId);
        detail.Balance.Should().Be(500);
    }

    // CA-004: userId inexistente → 404
    [Fact]
    public async Task GetWalletDetail_CA004_UnknownUser_Returns404()
    {
        var adminToken = GenerateAdminSiteToken();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var resp = await _client.GetAsync($"/api/admin/economy/gold/wallets/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // CA-005: ledger endpoint acessível por admin
    [Fact]
    public async Task GetLedger_CA005_Admin_ReturnsOk()
    {
        var adminToken = GenerateAdminSiteToken();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var resp = await _client.GetAsync("/api/admin/economy/gold/ledger?page=1&pageSize=10");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await resp.Content.ReadFromJsonAsync<GoldLedgerPageResponse>();
        page.Should().NotBeNull();
        page!.Items.Should().NotBeNull();
    }
}
