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

public class AuthRegisterEndpointTests : IAsyncLifetime
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

    [Fact]
    public async Task RegisterReturnsCreatedWithAuthResponseWhenDataIsValid()
    {
        var payload = new
        {
            email = "hunter@awaken.app",
            password = "Str0ngPass!",
            name = "Hunter",
            language = "pt-BR"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.User.Email.Should().Be("hunter@awaken.app");
        body.User.DisplayName.Should().Be("Hunter");
        body.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.User.AccessStatus.Should().Be("no_trial");
    }

    [Fact]
    public async Task RegisterReturnsValidationErrorWhenNameIsMissing()
    {
        var payload = new
        {
            email = "weak@awaken.app",
            password = "Str0ngPass!",
            language = "pt-BR"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RegisterReturnsConflictWhenEmailAlreadyExists()
    {
        var payload = new
        {
            email = "duplicate@awaken.app",
            password = "Str0ngPass!",
            name = "Hunter",
            language = "pt-BR"
        };

        var first = await _client.PostAsJsonAsync("/api/auth/register", payload);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync("/api/auth/register", payload);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await second.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Code.Should().Be("EMAIL_ALREADY_EXISTS");
    }

    [Fact]
    public async Task RegisterReturnsValidationErrorWhenPasswordIsTooShort()
    {
        var payload = new
        {
            email = "weak@awaken.app",
            password = "123",
            name = "Hunter",
            language = "pt-BR"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
