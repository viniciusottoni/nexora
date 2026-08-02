using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Shop;
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
/// Integration tests for US-178: Shop Catalog endpoint.
/// </summary>
public class ShopCatalogEndpointTests : IAsyncLifetime
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

        // "reforja_scroll" and "pedra_dungeon" are seeded by the
        // SeedLegacyShopProducts migration (US-188). Reuse those rows instead
        // of re-inserting them to avoid a duplicate-key violation on Key.
        var utcNow = DateTime.UtcNow;
        var inactiveProduct = await dbContext.ShopProducts
            .SingleAsync(p => p.Key == "pedra_dungeon");
        inactiveProduct.Deactivate(utcNow);
        await dbContext.SaveChangesAsync();

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
    public async Task GetProductsReturnsOnlyActiveProducts()
    {
        var token = await RegisterAndGetTokenAsync("catalog_active@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/shop/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<ShopProductResponse>>();
        products.Should().NotBeNull().And.NotBeEmpty();
        products.Should().Contain(p => p.Key == "reforja_scroll");
        products.Should().OnlyContain(p => p.Key != "pedra_dungeon");
    }

    [Fact]
    public async Task GetProductsDoesNotReturnInactiveProducts()
    {
        var token = await RegisterAndGetTokenAsync("catalog_inactive@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/shop/products");
        var products = await response.Content.ReadFromJsonAsync<List<ShopProductResponse>>();

        products.Should().NotContain(p => p.Key == "pedra_dungeon");
    }

    [Fact]
    public async Task UnauthenticatedGetProductsReturns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/shop/products");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
