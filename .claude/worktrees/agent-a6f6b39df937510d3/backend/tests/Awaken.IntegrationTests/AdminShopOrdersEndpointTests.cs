using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Common;
using Awaken.Contracts.Shop;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Entities.Audit;
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
/// US-193: testes de integração para visibilidade administrativa de compras.
/// Usa PostgreSQL real via Testcontainers.
///
/// CA-001: usuário sem role Admin → 403 Forbidden.
/// CA-002: admin vê pedidos existentes com canal, status e datas, sem dado de pagamento.
/// CA-003: acesso admin gera registro de AuditLog com ActorType = Admin.
/// </summary>
public class AdminShopOrdersEndpointTests : IAsyncLifetime
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

    private async Task<(string token, Guid userId)> RegisterAndGetTokenAsync(string email)
    {
        var payload = new { email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        return (auth.AccessToken, auth.User.Id);
    }

    private async Task<string> RegisterAdminAndGetTokenAsync(string email)
    {
        var payload = new { email, password = "Str0ngPass!", name = "Admin", language = "pt-BR" };
        await _client.PostAsJsonAsync("/api/auth/register", payload);

        // Promove para Admin diretamente no banco.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE users SET \"Role\" = 'Admin' WHERE \"Id\" = {0}", user.Id);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "Str0ngPass!" });
        var auth = (await loginResponse.Content.ReadFromJsonAsync<AuthResponse>())!;
        return auth.AccessToken;
    }

    private async Task<ShopOrder> SeedShopOrderAsync(
        Guid userId,
        string channel = "gold",
        string productKey = "reforja_scroll",
        string status = "granted",
        string? externalTransactionId = null,
        string? correlationId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var utcNow = DateTime.UtcNow;

        var order = ShopOrder.Create(userId, channel, productKey,
            externalTransactionId, correlationId ?? Guid.NewGuid().ToString(), utcNow);

        if (status == "granted") order.MarkGranted(utcNow);
        if (status == "failed") order.MarkFailed(utcNow);

        db.ShopOrders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }

    private async Task<int> CountAuditLogsAsync(string action, Guid? actorUserId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        return await db.AuditLogs
            .Where(a => a.Action == action &&
                        (actorUserId == null || a.ActorUserId == actorUserId))
            .CountAsync();
    }

    // ─── CA-001: não-admin recebe 403 ────────────────────────────────────────

    [Fact]
    public async Task GetOrders_WhenNonAdminAuthenticated_Returns403()
    {
        // CA-001: usuário regular autenticado não tem acesso.
        var (token, _) = await RegisterAndGetTokenAsync($"nonadmin-{Guid.NewGuid():N}@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/shop/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "CA-001: usuário sem role Admin deve receber 403");
    }

    [Fact]
    public async Task GetOrders_WhenUnauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/admin/shop/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "requisição sem token deve receber 401");
    }

    // ─── CA-002: admin vê pedidos com dados corretos ──────────────────────────

    [Fact]
    public async Task GetOrders_WhenAdminAndOrdersExist_Returns200WithCorrectData()
    {
        // CA-002: admin vê canal, status, datas e CorrelationId.
        var adminToken = await RegisterAdminAndGetTokenAsync($"admin-ca002-{Guid.NewGuid():N}@awaken.app");

        var (_, userId) = await RegisterAndGetTokenAsync($"user-ca002-{Guid.NewGuid():N}@awaken.app");
        var correlationId = Guid.NewGuid().ToString();
        await SeedShopOrderAsync(userId, "gold", "reforja_scroll", "granted",
            correlationId: correlationId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.GetAsync($"/api/admin/shop/orders?userId={userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "CA-002: admin deve receber 200 ao consultar pedidos");

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<AdminShopOrderResponse>>>();
        body.Should().NotBeNull();
        body!.CorrelationId.Should().NotBeNullOrWhiteSpace(
            "response deve incluir correlationId");

        var pagedResult = body.Data;
        pagedResult.Items.Should().HaveCount(1);

        var item = pagedResult.Items[0];
        item.Channel.Should().Be("gold");
        item.ProductKey.Should().Be("reforja_scroll");
        item.Status.Should().Be("granted");
        item.UserId.Should().Be(userId);
        item.CorrelationId.Should().Be(correlationId,
            "CA-002: CorrelationId deve estar presente para cruzamento com auditoria");
        item.GrantedAtUtc.Should().NotBeNull(
            "CA-002: GrantedAtUtc deve ser informado para pedidos concedidos");
    }

    [Fact]
    public async Task GetOrders_WhenAdminFiltersById_ReturnsOnlyMatchingOrders()
    {
        // CA-002: filtro por userId funciona corretamente.
        var adminToken = await RegisterAdminAndGetTokenAsync($"admin-filter-{Guid.NewGuid():N}@awaken.app");

        var (_, userId1) = await RegisterAndGetTokenAsync($"user-filter1-{Guid.NewGuid():N}@awaken.app");
        var (_, userId2) = await RegisterAndGetTokenAsync($"user-filter2-{Guid.NewGuid():N}@awaken.app");

        await SeedShopOrderAsync(userId1, "gold", "item_a", "granted");
        await SeedShopOrderAsync(userId2, "iap", "item_b", "granted");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.GetAsync($"/api/admin/shop/orders?userId={userId1}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<AdminShopOrderResponse>>>();
        body!.Data.Items.Should().AllSatisfy(o =>
            o.UserId.Should().Be(userId1, "filtro por userId deve retornar apenas pedidos daquele usuário"));
    }

    [Fact]
    public async Task GetOrders_WhenAdminFiltersIapOrders_ReturnsIapOrdersOnly()
    {
        // CA-002: filtro por canal funciona.
        var adminToken = await RegisterAdminAndGetTokenAsync($"admin-iap-{Guid.NewGuid():N}@awaken.app");

        var (_, userId) = await RegisterAndGetTokenAsync($"user-iap-{Guid.NewGuid():N}@awaken.app");
        await SeedShopOrderAsync(userId, "iap", "item_iap", "granted", "TXN-CA002-IAP-001");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.GetAsync("/api/admin/shop/orders?channel=iap");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<AdminShopOrderResponse>>>();
        body!.Data.Items.Should().NotBeEmpty();
        body.Data.Items.Should().AllSatisfy(o =>
            o.Channel.Should().Be("iap", "filtro de canal deve retornar apenas pedidos IAP"));
    }

    [Fact]
    public async Task GetOrders_WhenAdminQueriesIapOrder_ExternalTransactionIdIsPresent()
    {
        // CA-002 / RN-005: ExternalTransactionId é exposto como ID de referência.
        var adminToken = await RegisterAdminAndGetTokenAsync($"admin-extid-{Guid.NewGuid():N}@awaken.app");

        var (_, userId) = await RegisterAndGetTokenAsync($"user-extid-{Guid.NewGuid():N}@awaken.app");
        const string txnId = "TXN-CA002-EXTID-001";
        await SeedShopOrderAsync(userId, "iap", "premium_item", "granted", txnId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.GetAsync($"/api/admin/shop/orders?userId={userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<AdminShopOrderResponse>>>();
        body!.Data.Items.Should().HaveCount(1);
        body.Data.Items[0].ExternalTransactionId.Should().Be(txnId,
            "CA-002: ExternalTransactionId é ID de referência e deve estar visível ao admin");
    }

    // ─── CA-003: auditoria admin gerada ──────────────────────────────────────

    [Fact]
    public async Task GetOrders_WhenAdminQueries_GeneratesAuditLogWithAdminActorType()
    {
        // CA-003: consulta admin gera AuditLog com ActorType = Admin.
        var adminToken = await RegisterAdminAndGetTokenAsync($"admin-ca003-{Guid.NewGuid():N}@awaken.app");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.GetAsync("/api/admin/shop/orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verificar no banco que existe um AuditLog com Action = AdminShopOrders.Queried.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var auditEntry = await db.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "AdminShopOrders.Queried");

        auditEntry.Should().NotBeNull(
            "CA-003: consulta admin deve gerar AuditLog");
        auditEntry!.ActorType.Should().Be(AuditActorType.Admin,
            "CA-003: ActorType deve ser Admin");
        auditEntry.ActorUserId.Should().NotBeNull(
            "CA-003: ActorUserId deve ser o ID do admin");
    }

    [Fact]
    public async Task GetOrders_WhenAdminQueriesMultipleTimes_GeneratesAuditForEachQuery()
    {
        // CA-003: cada consulta gera um novo registro de auditoria.
        var adminToken = await RegisterAdminAndGetTokenAsync($"admin-multi-{Guid.NewGuid():N}@awaken.app");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        await _client.GetAsync("/api/admin/shop/orders");
        await _client.GetAsync("/api/admin/shop/orders");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var count = await db.AuditLogs
            .CountAsync(a => a.Action == "AdminShopOrders.Queried");

        count.Should().BeGreaterThanOrEqualTo(2,
            "CA-003: cada consulta admin deve gerar seu próprio AuditLog");
    }

    // ─── Paginação ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrders_PaginationParamsAreRespected()
    {
        var adminToken = await RegisterAdminAndGetTokenAsync($"admin-page-{Guid.NewGuid():N}@awaken.app");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.GetAsync("/api/admin/shop/orders?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<AdminShopOrderResponse>>>();
        body!.Data.Page.Should().Be(1);
        body.Data.PageSize.Should().Be(5);
    }
}
