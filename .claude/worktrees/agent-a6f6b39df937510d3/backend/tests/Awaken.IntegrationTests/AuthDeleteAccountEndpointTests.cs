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

public class AuthDeleteAccountEndpointTests : IAsyncLifetime
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

    private static HttpRequestMessage BuildDeleteAccountRequest(string accessToken, string? correlationId = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/delete-account")
        {
            Content = JsonContent.Create(new { confirmation = DeleteAccountRequest.ExpectedConfirmation })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (correlationId is not null)
            request.Headers.Add("X-Correlation-Id", correlationId);
        return request;
    }

    [Fact]
    public async Task DeleteAccountReturnsOkWhenAuthenticatedAndConfirmed()
    {
        var auth = await RegisterAndLoginAsync("delete1@awaken.app");

        var response = await _client.SendAsync(BuildDeleteAccountRequest(auth.AccessToken, "delete-corr-1"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("accountStatus").GetString().Should().Be("deleted");
        document.RootElement.GetProperty("correlationId").GetString().Should().Be("delete-corr-1");
    }

    [Fact]
    public async Task DeleteAccountReturnsUnauthorizedWithoutToken()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/delete-account")
        {
            Content = JsonContent.Create(new { confirmation = DeleteAccountRequest.ExpectedConfirmation })
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccountReturnsBadRequestWhenConfirmationIsInvalid()
    {
        var auth = await RegisterAndLoginAsync("delete-bad@awaken.app");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/delete-account")
        {
            Content = JsonContent.Create(new { confirmation = "WRONG_CONFIRMATION" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("CONFIRMATION_REQUIRED");
        document.RootElement.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task DeleteAccountRevokesRefreshTokenSoItCannotBeReused()
    {
        var auth = await RegisterAndLoginAsync("delete-revoke@awaken.app");

        await _client.SendAsync(BuildDeleteAccountRequest(auth.AccessToken));

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh-token", new
        {
            refreshToken = auth.RefreshToken
        });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccountSoftDeletesUserSoLoginFails()
    {
        var email = "delete-login@awaken.app";
        const string password = "Str0ngPass!";
        var auth = await RegisterAndLoginAsync(email, password);

        await _client.SendAsync(BuildDeleteAccountRequest(auth.AccessToken));

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
