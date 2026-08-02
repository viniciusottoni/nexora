using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Common;
using Awaken.Contracts.Economy;
using Awaken.Contracts.Shop;
using Awaken.Domain.Entities.Shop;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

/// <summary>
/// Integration tests for US-179: IAP Transaction Ledger (idempotency).
/// US-226: compra de pacote de Gold via IAP credita a carteira corretamente.
/// </summary>
public class IapPurchaseTests : IAsyncLifetime
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
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AwakenDbContext>));
                if (descriptor is not null)
                    services.Remove(descriptor);

                services.AddDbContext<AwakenDbContext>(options => options.UseNpgsql(pgConnectionString));

                // A validação de transação agora chama o RevenueCat de verdade
                // (GET /v1/subscribers/{appUserId}); estes testes exercitam
                // idempotência e crédito, então o provider é substituído por um
                // fake que aprova toda transação em sandbox.
                services.AddScoped<IRevenueCatValidationService, FakeRevenueCatValidationService>();
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

    /// <summary>
    /// IAP catalog endpoint returns only active products.
    /// </summary>
    [Fact]
    public async Task GetShopProductsReturnsActiveProducts()
    {
        var token = await RegisterAndGetTokenAsync("iap_catalog@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/shop/products");

        // The catalog is empty by default (no seed data), should return 200 with empty list
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<ShopProductResponse>>();
        products.Should().NotBeNull();
    }

    /// <summary>
    /// Idempotency: processing the same transaction twice grants the item only once.
    /// </summary>
    [Fact]
    public async Task SameTransactionProcessedTwiceGrantsItemOnlyOnce()
    {
        var token = await RegisterAndGetTokenAsync("iap_idempotency@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Seed a shop product directly into the database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var product = Awaken.Domain.Entities.Shop.ShopProduct.Create(
                "reforja_scroll_iap", "Pergaminho da Reforja", null,
                "consumable", "rare", "rc_reforja_scroll", DateTime.UtcNow);
            db.ShopProducts.Add(product);
            await db.SaveChangesAsync();
        }

        var request = new ProcessIapPurchaseRequest("TXN-IDEMPOTENCY-001", "reforja_scroll_iap", "google_play");

        // First call
        var response1 = await _client.PostAsJsonAsync("/api/v1/shop/iap/process", request);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second call with same transaction ID
        var response2 = await _client.PostAsJsonAsync("/api/v1/shop/iap/process", request);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Only one ledger entry should exist for this transaction
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var ledgerCount = await verifyDb.IapTransactionLedgers
            .CountAsync(l => l.TransactionId == "TXN-IDEMPOTENCY-001");
        ledgerCount.Should().Be(1);

        // The single ledger entry should be in "granted" status
        var ledger = await verifyDb.IapTransactionLedgers
            .FirstAsync(l => l.TransactionId == "TXN-IDEMPOTENCY-001");
        ledger.Status.Should().Be("granted");
    }

    // ─── US-226: compra de pacote de Gold credita saldo correto ────────────────

    private async Task<(Guid UserId, string AccessToken)> RegisterUserAsync(string email)
    {
        var token = await RegisterAndGetTokenAsync(email);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var userId = await db.Users
            .Where(u => u.Email == email)
            .Select(u => u.Id)
            .FirstAsync();

        return (userId, token);
    }

    private async Task SeedGoldPackProductAsync(string key, int goldAmount)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var product = ShopProduct.Create(
            key, "Pacote de Gold (teste)", null,
            "consumable", "rare", "rc_gold_pack_test", DateTime.UtcNow,
            priceGold: null, goldAmount: goldAmount);
        db.ShopProducts.Add(product);
        await db.SaveChangesAsync();
    }

    private async Task<long> GetWalletBalanceAsync(string accessToken)
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("/api/economy/wallet");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<WalletResponse>>();
        return body!.Data.Balance;
    }

    /// <summary>
    /// US-226 (RN-001/RN-002/RN-007): compra de pacote de Gold válida credita
    /// exatamente o valor definido em ShopProduct.GoldAmount na carteira do
    /// usuário, refletido em GET /api/economy/wallet — nunca um valor vindo do
    /// payload da requisição (que não tem nenhum campo de quantidade).
    /// </summary>
    [Fact]
    public async Task GoldPackPurchase_WhenValidated_CreditsCorrectBalanceReflectedInWalletEndpoint()
    {
        // Arrange
        var (userId, token) = await RegisterUserAsync("iap_gold_pack@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await SeedGoldPackProductAsync("gold_pack_500_test", goldAmount: 500);

        var request = new ProcessIapPurchaseRequest("TXN-GOLD-PACK-001", "gold_pack_500_test", "google_play");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/shop/iap/process", request);

        // Assert — resposta rica com status seguro (US-226 seção 10).
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ShopOrderResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("granted");
        result.Channel.Should().Be("iap");

        // Assert — saldo refletido no endpoint de carteira (não direto no banco).
        var balance = await GetWalletBalanceAsync(token);
        balance.Should().Be(500);

        // Assert — rastreabilidade: GoldLedgerEntry referencia o ShopOrder (RN-007).
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var order = await verifyDb.ShopOrders.FirstAsync(o => o.ExternalTransactionId == "TXN-GOLD-PACK-001");
        var ledgerEntry = await verifyDb.GoldLedgerEntries
            .FirstAsync(l => l.ReferenceType == "shop_order" && l.ReferenceId == order.Id.ToString());
        ledgerEntry.Amount.Should().Be(500);
    }

    /// <summary>
    /// US-226 (RN-003/RN-004): processar a mesma transação de compra de Gold
    /// duas vezes não duplica o crédito — saldo permanece o mesmo após a
    /// segunda chamada.
    /// </summary>
    [Fact]
    public async Task GoldPackPurchase_WhenSameTransactionProcessedTwice_DoesNotDuplicateCredit()
    {
        // Arrange
        var (userId, token) = await RegisterUserAsync("iap_gold_pack_idempotency@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await SeedGoldPackProductAsync("gold_pack_250_test", goldAmount: 250);

        var request = new ProcessIapPurchaseRequest("TXN-GOLD-PACK-DUP-001", "gold_pack_250_test", "google_play");

        // Act — duas chamadas com a mesma transação externa.
        var response1 = await _client.PostAsJsonAsync("/api/v1/shop/iap/process", request);
        var response2 = await _client.PostAsJsonAsync("/api/v1/shop/iap/process", request);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var balance = await GetWalletBalanceAsync(token);
        balance.Should().Be(250, "RN-004: mesma transação externa não pode creditar Gold duas vezes");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var orderCount = await verifyDb.ShopOrders
            .CountAsync(o => o.ExternalTransactionId == "TXN-GOLD-PACK-DUP-001");
        orderCount.Should().Be(1, "idempotência: um único ShopOrder por transação externa");
    }
}
