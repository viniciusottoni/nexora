using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Awaken.Contracts.Auth;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class AuthLogoutEndpointTests : IAsyncLifetime
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

    private async Task<AuthResponse> RegisterAndLoginAsync(
        string email = "hunter@awaken.app",
        string password = "Str0ngPass!")
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            name = "Hunter",
            language = "pt-BR"
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        return (await loginResponse.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    [Fact]
    public async Task LogoutReturnsOkWhenAuthenticated()
    {
        var auth = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        _client.DefaultRequestHeaders.Add("X-Correlation-Id", "logout-corr-1");

        var response = await _client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("correlationId").GetString().Should().Be("logout-corr-1");
    }

    [Fact]
    public async Task LogoutReturnsUnauthorizedWithoutToken()
    {
        var response = await _client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutRevokesRefreshTokenSoItCannotBeReused()
    {
        var auth = await RegisterAndLoginAsync("revoke@awaken.app");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        await _client.PostAsync("/api/auth/logout", null);
        _client.DefaultRequestHeaders.Authorization = null;

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh-token", new
        {
            refreshToken = auth.RefreshToken
        });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MultipleLogoutsAreIdempotent()
    {
        var auth = await RegisterAndLoginAsync("idempotent@awaken.app");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var first = await _client.PostAsync("/api/auth/logout", null);
        var second = await _client.PostAsync("/api/auth/logout", null);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
