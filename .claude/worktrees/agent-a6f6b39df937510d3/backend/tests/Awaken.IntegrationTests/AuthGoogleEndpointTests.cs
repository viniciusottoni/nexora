using System.Net;
using System.Net.Http.Json;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Common;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    public Task<GoogleTokenPayload?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (idToken == "invalid-token")
            return Task.FromResult<GoogleTokenPayload?>(null);

        return Task.FromResult<GoogleTokenPayload?>(new GoogleTokenPayload(
            $"google-sub-{idToken}",
            $"{idToken}@awaken.app",
            true,
            "Hunter",
            null));
    }
}

public class AuthGoogleEndpointTests : IAsyncLifetime
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
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IGoogleTokenValidator, FakeGoogleTokenValidator>();
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

    [Fact]
    public async Task GoogleReturnsOkAndCreatesNewUserWhenEmailDoesNotExist()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/google", new
        {
            provider = "google",
            providerCredential = "new-hunter"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.User.Email.Should().Be("new-hunter@awaken.app");
        body.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GoogleReturnsOkAndReusesSameUserOnSecondLogin()
    {
        var first = await _client.PostAsJsonAsync("/api/auth/google", new
        {
            provider = "google",
            providerCredential = "returning-hunter"
        });
        var firstBody = await first.Content.ReadFromJsonAsync<AuthResponse>();

        var second = await _client.PostAsJsonAsync("/api/auth/google", new
        {
            provider = "google",
            providerCredential = "returning-hunter"
        });
        var secondBody = await second.Content.ReadFromJsonAsync<AuthResponse>();

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        secondBody!.User.Id.Should().Be(firstBody!.User.Id);
    }

    [Fact]
    public async Task GoogleLinksExistingLocalAccountByEmail()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "linked-hunter@awaken.app",
            password = "Str0ngPass!",
            name = "Hunter",
            language = "pt-BR"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/google", new
        {
            provider = "google",
            providerCredential = "linked-hunter"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.User.Email.Should().Be("linked-hunter@awaken.app");
    }

    [Fact]
    public async Task GoogleReturnsUnauthorizedWhenProviderTokenIsInvalid()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/google", new
        {
            provider = "google",
            providerCredential = "invalid-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Code.Should().Be("GOOGLE_AUTH_FAILED");
    }

    [Fact]
    public async Task GoogleReturnsValidationErrorWhenProviderCredentialIsMissing()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/google", new
        {
            provider = "google"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
