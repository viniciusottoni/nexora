using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Inventory;
using Awaken.Domain.Entities.Inventory;
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
/// Integration tests for US-233: inventario padrao vazio (CA-001/CA-002).
/// Um usuario recem-criado nunca deve receber itens automaticamente,
/// independente do plano de assinatura (trial, mensal ou anual).
/// </summary>
public class InventoryEmptyByDefaultEndpointTests : IAsyncLifetime
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

    private static string UniqueEmail(string prefix) =>
        $"{prefix}_{Guid.NewGuid():N}@awaken.app";

    [Fact]
    public async Task GetInventoryItem_ReturnsZero_ForBrandNewUser_WithoutCreatingAnyRow()
    {
        // CA-001: usuario recem-criado nao deve receber nenhum item automaticamente.
        var email = UniqueEmail("inventory_empty_new");
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/inventory/items/{ItemKeys.ReforgeScroll}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<InventoryItemResponse>();
        body.Should().NotBeNull();
        body!.ItemKey.Should().Be(ItemKeys.ReforgeScroll);
        body.Quantity.Should().Be(0);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        var items = await dbContext.InventoryItems.Where(i => i.UserId == user.Id).ToListAsync();
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetInventoryItem_ReturnsZero_AfterStartingTrial_WithoutGrantingAnyItem()
    {
        // CA-002: iniciar um plano (trial, mensal ou anual) nunca deve conceder itens implicitamente.
        var email = UniqueEmail("inventory_empty_trial");
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var trialResponse = await _client.PostAsync("/api/subscriptions/trial/start", null);
        trialResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _client.GetAsync($"/api/inventory/items/{ItemKeys.DungeonStone}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<InventoryItemResponse>();
        body!.Quantity.Should().Be(0);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        var items = await dbContext.InventoryItems.Where(i => i.UserId == user.Id).ToListAsync();
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task UnauthenticatedGetInventoryItem_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync($"/api/inventory/items/{ItemKeys.ReforgeScroll}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
