using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Common;
using Awaken.Contracts.Economy;
using Awaken.Domain.Repositories;
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
/// Integration tests for US-192: GET /api/economy/transactions.
/// CA-001: usuario ve seus lancamentos com dados corretos.
/// CA-002: paginacao funcional.
/// Acesso negado sem autenticacao.
/// </summary>
public class TransactionsEndpointTests : IAsyncLifetime
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

    private async Task<(string Token, Guid UserId)> RegisterAndGetAsync(string email)
    {
        var payload = new { email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;

        using var scope = _factory.Services.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = await userRepo.GetByEmailAsync(email);
        return (auth.AccessToken, user!.Id);
    }

    // ─────────────────────────────────────────────────────────────────────
    // CA-001: usuario vê seus lançamentos
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTransactions_CA001_ReturnsGoldMovements_ForAuthenticatedUser()
    {
        var (token, userId) = await RegisterAndGetAsync("tx_ca001_gold@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Cria movimentações de Gold via serviço.
        using (var scope = _factory.Services.CreateScope())
        {
            var walletService = scope.ServiceProvider.GetRequiredService<IGoldWalletService>();
            await walletService.CreditAsync(userId, 100, "quest_reward");
            await walletService.DebitAsync(userId, 30, "shop_purchase");
        }

        var response = await _client.GetAsync("/api/economy/transactions?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<TransactionPageResponse>>();
        body.Should().NotBeNull();
        body!.Data.Items.Should().HaveCount(2);
        body.Data.TotalCount.Should().Be(2);
        body.Data.HasMore.Should().BeFalse();
        body.CorrelationId.Should().NotBeNullOrWhiteSpace();

        var creditItem = body.Data.Items.FirstOrDefault(i => i.Direction == "credit");
        creditItem.Should().NotBeNull();
        creditItem!.Amount.Should().Be(100);
        creditItem.Type.Should().Be("gold_movement");

        var debitItem = body.Data.Items.FirstOrDefault(i => i.Direction == "debit");
        debitItem.Should().NotBeNull();
        debitItem!.Amount.Should().Be(30);
    }

    [Fact]
    public async Task GetTransactions_CA001_ReturnsEmptyList_WhenNoHistory()
    {
        var (token, _) = await RegisterAndGetAsync("tx_ca001_empty@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/economy/transactions?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<TransactionPageResponse>>();
        body!.Data.Items.Should().BeEmpty();
        body.Data.TotalCount.Should().Be(0);
    }

    // ─────────────────────────────────────────────────────────────────────
    // CA-002: paginação funcional
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTransactions_CA002_PaginationWorksWithoutDuplication()
    {
        var (token, userId) = await RegisterAndGetAsync("tx_ca002_page@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Cria 5 movimentações de Gold.
        using (var scope = _factory.Services.CreateScope())
        {
            var walletService = scope.ServiceProvider.GetRequiredService<IGoldWalletService>();
            for (var i = 1; i <= 5; i++)
                await walletService.CreditAsync(userId, i * 10, $"reward_{i}");
        }

        var page1 = await _client.GetAsync("/api/economy/transactions?page=1&pageSize=3");
        var page2 = await _client.GetAsync("/api/economy/transactions?page=2&pageSize=3");

        page1.StatusCode.Should().Be(HttpStatusCode.OK);
        page2.StatusCode.Should().Be(HttpStatusCode.OK);

        var body1 = await page1.Content.ReadFromJsonAsync<ApiResponse<TransactionPageResponse>>();
        var body2 = await page2.Content.ReadFromJsonAsync<ApiResponse<TransactionPageResponse>>();

        body1!.Data.Items.Should().HaveCount(3);
        body2!.Data.Items.Should().HaveCount(2);
        body1.Data.HasMore.Should().BeTrue();
        body2.Data.HasMore.Should().BeFalse();
        body1.Data.TotalCount.Should().Be(5);

        // Sem duplicatas entre páginas.
        var allIds = body1.Data.Items.Select(i => i.Id)
            .Concat(body2.Data.Items.Select(i => i.Id))
            .ToList();
        allIds.Should().OnlyHaveUniqueItems();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Acesso negado sem autenticação
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTransactions_Returns401_WhenUnauthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/economy/transactions");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
