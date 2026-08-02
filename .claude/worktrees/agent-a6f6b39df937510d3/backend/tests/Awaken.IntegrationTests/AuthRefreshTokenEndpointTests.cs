using System.Net;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Common;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class AuthRefreshTokenEndpointTests : IAsyncLifetime
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
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task<AuthResponse> RegisterAndLoginAsync(string email = "hunter@awaken.app")
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Str0ngPass!",
            name = "Hunter",
            language = "pt-BR"
        });
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Str0ngPass!"
        });
        return (await loginResp.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    [Fact]
    public async Task RefreshTokenReturnsNewTokenPairOnValidToken()
    {
        var auth = await RegisterAndLoginAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", new
        {
            refreshToken = auth.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBe(auth.RefreshToken);
        body.User.AccessStatus.Should().Be("no_trial");
    }

    [Fact]
    public async Task RefreshTokenRejectsReuseOfRotatedToken()
    {
        var auth = await RegisterAndLoginAsync("hunter2@awaken.app");

        await _client.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken = auth.RefreshToken });

        var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken = auth.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Code.Should().Be("SESSION_INVALID");
    }

    [Fact]
    public async Task RefreshTokenReturnsUnauthorizedForInvalidToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", new
        {
            refreshToken = "invalid-token-that-does-not-exist"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Code.Should().Be("SESSION_INVALID");
    }
}
