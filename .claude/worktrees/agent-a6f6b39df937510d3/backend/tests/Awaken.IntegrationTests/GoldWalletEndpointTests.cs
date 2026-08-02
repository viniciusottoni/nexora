using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Common;
using Awaken.Contracts.Economy;
using Awaken.Domain.Entities.Economy;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

/// <summary>
/// Integration tests for US-186: Gold wallet foundation (GET /api/economy/wallet).
/// </summary>
public class GoldWalletEndpointTests : IAsyncLifetime
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

    private async Task<string> RegisterAndGetTokenAsync(string email)
    {
        var payload = new { email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        return auth.AccessToken;
    }

    [Fact]
    public async Task GetWallet_ReturnsZeroBalance_AndCreatesWallet_WhenUserNeverHadOne()
    {
        // CA-001: usuario que nunca teve carteira deve receber saldo 0 sem erro.
        var token = await RegisterAndGetTokenAsync("wallet_lazy@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/economy/wallet");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<WalletResponse>>();
        body.Should().NotBeNull();
        body!.Data.Balance.Should().Be(0);
        body.CorrelationId.Should().NotBeNullOrWhiteSpace();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var wallets = await dbContext.GoldWallets.ToListAsync();
        wallets.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetWallet_ReturnsSameWallet_OnSubsequentCalls_WithoutDuplicating()
    {
        var token = await RegisterAndGetTokenAsync("wallet_idempotent@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.GetAsync("/api/economy/wallet");
        await _client.GetAsync("/api/economy/wallet");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var wallets = await dbContext.GoldWallets.ToListAsync();
        wallets.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetWallet_ReflectsBalance_ReconciledFromLedger()
    {
        // CA-003: saldo deve ser igual a soma do ledger.
        var token = await RegisterAndGetTokenAsync("wallet_reconcile@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Cria a carteira via primeiro acesso.
        await _client.GetAsync("/api/economy/wallet");

        using (var scope = _factory.Services.CreateScope())
        {
            var walletService = scope.ServiceProvider
                .GetRequiredService<Awaken.Application.Common.Interfaces.IGoldWalletService>();
            var userRepository = scope.ServiceProvider.GetRequiredService<Awaken.Domain.Repositories.IUserRepository>();
            var user = await userRepository.GetByEmailAsync("wallet_reconcile@awaken.app");

            await walletService.CreditAsync(user!.Id, 100, "quest_reward");
            await walletService.CreditAsync(user.Id, 50, "streak_bonus");
            await walletService.DebitAsync(user.Id, 30, "shop_purchase");
        }

        var response = await _client.GetAsync("/api/economy/wallet");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<WalletResponse>>();
        body!.Data.Balance.Should().Be(120);

        using var verifyScope = _factory.Services.CreateScope();
        var dbContext = verifyScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var wallet = await dbContext.GoldWallets.FirstAsync();
        var ledgerSum = await dbContext.GoldLedgerEntries
            .Where(e => e.WalletId == wallet.Id)
            .SumAsync(e => e.Direction == GoldLedgerDirection.Credit ? e.Amount : -e.Amount);

        ledgerSum.Should().Be(wallet.Balance);
    }

    [Fact]
    public async Task UnauthenticatedGetWallet_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/economy/wallet");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
