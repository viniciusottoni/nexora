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

public class AuthLoginEndpointTests : IAsyncLifetime
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

    private async Task RegisterAsync(string email, string password, string name = "Hunter")
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            name,
            language = "pt-BR"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task LoginReturnsOkWithAuthResponseWhenCredentialsAreValid()
    {
        await RegisterAsync("hunter@awaken.app", "Str0ngPass!");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "hunter@awaken.app",
            password = "Str0ngPass!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.User.Email.Should().Be("hunter@awaken.app");
        body.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.User.AccessStatus.Should().Be("no_trial");
    }

    [Fact]
    public async Task LoginReturnsUnauthorizedWhenPasswordIsWrong()
    {
        await RegisterAsync("wrongpass@awaken.app", "Str0ngPass!");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "wrongpass@awaken.app",
            password = "Incorrect1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Code.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task LoginReturnsUnauthorizedWhenAccountDoesNotExist()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "doesnotexist@awaken.app",
            password = "Str0ngPass!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Code.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task LoginReturnsValidationErrorWhenEmailIsMissing()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            password = "Str0ngPass!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task LoginReturnsValidationErrorWhenPasswordIsMissing()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "hunter@awaken.app"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
