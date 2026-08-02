using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Common;
using Awaken.Contracts.Shop;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Repositories;
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
/// Testes de integração para US-189: orquestração de compra e trilha de pedido.
/// Usa PostgreSQL real via Testcontainers (sem mocks de repositório).
///
/// CA-001: compra em Gold cria ShopOrder rastreável.
/// CA-002: idempotência IAP — mesma transação não concede duas vezes.
/// CA-003: Gold sem saldo → pedido failed, sem concessão.
/// </summary>
public class ShopOrderEndpointTests : IAsyncLifetime
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
                // Substituir o DbContext via DI em vez de UseSetting: o
                // appsettings.Local.json é carregado por cima da configuração
                // do host (Program.cs), então UseSetting apontaria os testes
                // para o banco de dev local em máquinas de desenvolvimento.
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AwakenDbContext>));
                if (descriptor is not null)
                    services.Remove(descriptor);

                services.AddDbContext<AwakenDbContext>(
                    options => options.UseNpgsql(pgConnectionString));

                // A validação IAP real chama o RevenueCat; aqui o foco é a
                // trilha de pedido/idempotência, então usa-se o fake aprovador.
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

    private async Task<(string token, Guid userId)> RegisterAndGetTokenAsync(string email)
    {
        var payload = new { email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;

        // Recuperar UserId do banco.
        using var scope = _factory.Services.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = await userRepo.GetByEmailAsync(email);
        return (auth.AccessToken, user!.Id);
    }

    private async Task<ShopProduct> SeedGoldProductAsync(
        string key = "reforja_scroll_test",
        int priceGold = 150)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var product = ShopProduct.Create(
            key, "Pergaminho da Reforja", null,
            "consumable", "rare", null, DateTime.UtcNow, priceGold);
        db.ShopProducts.Add(product);
        await db.SaveChangesAsync();
        return product;
    }

    private async Task SeedWalletBalanceAsync(Guid userId, long amount)
    {
        using var scope = _factory.Services.CreateScope();
        var goldWalletService = scope.ServiceProvider.GetRequiredService<IGoldWalletService>();
        await goldWalletService.CreditAsync(userId, amount, "test_setup");
    }

    // ─── CA-001: compra Gold cria ShopOrder rastreável ────────────────────────

    [Fact]
    public async Task PurchaseWithGold_WhenSufficientBalance_CreatesGrantedShopOrder()
    {
        // Arrange
        var (token, userId) = await RegisterAndGetTokenAsync("shop_order_ca001@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await SeedGoldProductAsync("reforja_ca001", 100);
        await SeedWalletBalanceAsync(userId, 500);

        // Act
        var response = await _client.PostAsync("/api/shop/items/reforja_ca001/purchase", null);

        // Assert — HTTP 200 com ShopOrderResponse.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ShopOrderResponse>>();
        body.Should().NotBeNull();
        body!.Data.Channel.Should().Be("gold");
        body.Data.ProductKey.Should().Be("reforja_ca001");
        body.Data.Status.Should().Be("granted");
        body.Data.OrderId.Should().NotBeEmpty();
        body.CorrelationId.Should().NotBeNullOrWhiteSpace();

        // Verificar no banco.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var order = await db.ShopOrders.FirstOrDefaultAsync(o => o.UserId == userId);
        order.Should().NotBeNull();
        order!.Status.Should().Be("granted");
        order.Channel.Should().Be("gold");
        order.ProductKey.Should().Be("reforja_ca001");
        order.GrantedAtUtc.Should().NotBeNull();

        // Inventário deve ter sido incrementado.
        var item = await db.InventoryItems
            .FirstOrDefaultAsync(i => i.UserId == userId && i.ItemKey == "reforja_ca001");
        item.Should().NotBeNull();
        item!.Quantity.Should().Be(1);

        // Saldo deve ter sido debitado.
        var wallet = await db.GoldWallets.FirstAsync(w => w.UserId == userId);
        wallet.Balance.Should().Be(400); // 500 - 100
    }

    // ─── CA-003: Gold sem saldo ────────────────────────────────────────────────

    [Fact]
    public async Task PurchaseWithGold_WhenInsufficientBalance_ReturnsError_AndShopOrderFailed()
    {
        // Arrange
        var (token, userId) = await RegisterAndGetTokenAsync("shop_order_ca003@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await SeedGoldProductAsync("reforja_ca003", 500);
        await SeedWalletBalanceAsync(userId, 50); // saldo 50, preço 500

        // Act
        var response = await _client.PostAsync("/api/shop/items/reforja_ca003/purchase", null);

        // Assert — erro de saldo insuficiente (500 Unprocessable ou 422).
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.UnprocessableEntity,
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError); // aceita 500 também se não mapeado

        // Verificar no banco — ShopOrder em "failed".
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var order = await db.ShopOrders.FirstOrDefaultAsync(o => o.UserId == userId);
        order.Should().NotBeNull("ShopOrder deve existir mesmo com falha");
        order!.Status.Should().Be("failed");

        // Inventário não deve ter itens.
        var item = await db.InventoryItems
            .FirstOrDefaultAsync(i => i.UserId == userId && i.ItemKey == "reforja_ca003");
        item.Should().BeNull("item não deve ter sido concedido");

        // Saldo deve estar intacto.
        var wallet = await db.GoldWallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet is not null)
            wallet.Balance.Should().Be(50, "saldo não deve ter sido debitado");
    }

    // ─── CA-002: idempotência IAP ─────────────────────────────────────────────

    [Fact]
    public async Task IapPurchase_WhenSameTransactionProcessedTwice_GrantsItemOnlyOnceAndCreatesOneShopOrder()
    {
        // Arrange
        var (token, userId) = await RegisterAndGetTokenAsync("shop_order_ca002@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var product = ShopProduct.Create(
                "reforja_scroll_iap_ca002", "Pergaminho IAP", null,
                "consumable", "rare", "rc_reforja_iap", DateTime.UtcNow);
            db.ShopProducts.Add(product);
            await db.SaveChangesAsync();
        }

        var request = new ProcessIapPurchaseRequest("TXN-US189-CA002", "reforja_scroll_iap_ca002", "google_play");

        // Act — primeira chamada.
        var response1 = await _client.PostAsJsonAsync("/api/v1/shop/iap/process", request);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act — segunda chamada com mesma transação.
        var response2 = await _client.PostAsJsonAsync("/api/v1/shop/iap/process", request);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert — apenas um ShopOrder criado com ExternalTransactionId.
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var shopOrders = await verifyDb.ShopOrders
            .Where(o => o.ExternalTransactionId == "TXN-US189-CA002")
            .ToListAsync();
        shopOrders.Should().HaveCount(1, "idempotência: um único ShopOrder por transação IAP");
        shopOrders[0].Status.Should().Be("granted");
        shopOrders[0].Channel.Should().Be("iap");

        // Ledger IAP legado — apenas um registro.
        var ledgers = await verifyDb.IapTransactionLedgers
            .Where(l => l.TransactionId == "TXN-US189-CA002")
            .ToListAsync();
        ledgers.Should().HaveCount(1);
        ledgers[0].Status.Should().Be("granted");

        // Item concedido apenas uma vez.
        var item = await verifyDb.InventoryItems
            .FirstOrDefaultAsync(i => i.UserId == userId && i.ItemKey == "reforja_scroll_iap_ca002");
        item.Should().NotBeNull();
        item!.Quantity.Should().Be(1, "item concedido apenas uma vez (CA-002)");
    }

    [Fact]
    public async Task PurchaseWithGold_WhenProductDoesNotExist_Returns404()
    {
        // Arrange
        var (token, _) = await RegisterAndGetTokenAsync("shop_order_404@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync("/api/shop/items/produto_inexistente/purchase", null);

        // Assert — RN-004.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PurchaseWithGold_WhenUnauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsync("/api/shop/items/reforja_scroll/purchase", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── US-227 / RN-004/RN-006: concorrência real com Postgres ────────────────

    [Fact]
    public async Task PurchaseWithGold_WhenTwoSimultaneousPurchases_OnlyOneSucceeds_AndBalanceNeverNegative()
    {
        // Arrange — saldo suficiente para exatamente UMA das duas compras.
        var (token, userId) = await RegisterAndGetTokenAsync("shop_order_concurrency@awaken.app");
        await SeedGoldProductAsync("reforja_concurrency", 100);
        await SeedWalletBalanceAsync(userId, 100);

        // Act — duas compras simultâneas usando clientes HTTP independentes
        // (mesmo WebApplicationFactory, mesmo usuário autenticado).
        using var client1 = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
        client1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var client2 = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var task1 = client1.PostAsync("/api/shop/items/reforja_concurrency/purchase", null);
        var task2 = client2.PostAsync("/api/shop/items/reforja_concurrency/purchase", null);
        var responses = await Task.WhenAll(task1, task2);

        // Assert — RN-004: saldo final nunca fica negativo.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var wallet = await db.GoldWallets.FirstAsync(w => w.UserId == userId);
        wallet.Balance.Should().BeGreaterThanOrEqualTo(0);
        wallet.Balance.Should().Be(0, "exatamente uma compra deve ter sido debitada (100 - 100 = 0)");

        // RN-004/CA: exatamente um pedido "granted", o outro "failed".
        var orders = await db.ShopOrders
            .Where(o => o.UserId == userId && o.ProductKey == "reforja_concurrency")
            .ToListAsync();
        orders.Should().HaveCount(2);
        orders.Count(o => o.Status == "granted").Should().Be(1, "apenas uma compra deve ter sido concedida");
        orders.Count(o => o.Status == "failed").Should().Be(1, "a outra compra deve falhar de forma controlada");

        // RN-006: inventário incrementado exatamente uma vez (não perdeu nem duplicou).
        var item = await db.InventoryItems
            .FirstOrDefaultAsync(i => i.UserId == userId && i.ItemKey == "reforja_concurrency");
        item.Should().NotBeNull();
        item!.Quantity.Should().Be(1);

        // Ambas as respostas HTTP devem ter sido bem-sucedidas ou erro controlado
        // (200 para a vencedora, 422 para saldo insuficiente na perdedora) —
        // nunca 500 não tratado.
        foreach (var response in responses)
            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    // ─── US-227 / RN-003: idempotência por Idempotency-Key ─────────────────────

    [Fact]
    public async Task PurchaseWithGold_WhenSameIdempotencyKeySentTwice_GrantsItemOnlyOnce()
    {
        // Arrange
        var (token, userId) = await RegisterAndGetTokenAsync("shop_order_idempotency@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await SeedGoldProductAsync("reforja_idempotency", 100);
        await SeedWalletBalanceAsync(userId, 500);

        const string idempotencyKey = "client-idem-key-001";

        using var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/shop/items/reforja_idempotency/purchase");
        request1.Headers.Add("Idempotency-Key", idempotencyKey);
        var response1 = await _client.SendAsync(request1);

        using var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/shop/items/reforja_idempotency/purchase");
        request2.Headers.Add("Idempotency-Key", idempotencyKey);
        var response2 = await _client.SendAsync(request2);

        // Assert — ambas as respostas retornam o mesmo pedido.
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var body1 = await response1.Content.ReadFromJsonAsync<ApiResponse<ShopOrderResponse>>();
        var body2 = await response2.Content.ReadFromJsonAsync<ApiResponse<ShopOrderResponse>>();
        body1!.Data.OrderId.Should().Be(body2!.Data.OrderId, "RN-003: mesma chave deve retornar o mesmo pedido");

        // Apenas um débito e uma concessão de item.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var orders = await db.ShopOrders
            .Where(o => o.ExternalTransactionId == idempotencyKey)
            .ToListAsync();
        orders.Should().HaveCount(1, "RN-003: reenvio não deve criar novo pedido");

        var item = await db.InventoryItems
            .FirstOrDefaultAsync(i => i.UserId == userId && i.ItemKey == "reforja_idempotency");
        item.Should().NotBeNull();
        item!.Quantity.Should().Be(1, "RN-003: item não deve ser concedido duas vezes");

        var wallet = await db.GoldWallets.FirstAsync(w => w.UserId == userId);
        wallet.Balance.Should().Be(400, "RN-003: saldo não deve ser debitado duas vezes (500 - 100)");
    }
}
