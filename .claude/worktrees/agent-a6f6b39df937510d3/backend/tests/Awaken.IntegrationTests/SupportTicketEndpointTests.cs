using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

/// <summary>
/// Integration tests for US-176: Support Ticket endpoint.
/// </summary>
public class SupportTicketEndpointTests : IAsyncLifetime
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
        var dbContext = scope.ServiceProvider.GetRequiredService<Awaken.Infrastructure.Persistence.AwakenDbContext>();
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

    /// <summary>CA-001: Authenticated user can create ticket (returns 201).</summary>
    [Fact]
    public async Task AuthenticatedUserCanCreateSupportTicket()
    {
        var token = await RegisterAndGetTokenAsync("support_ca001@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateSupportTicketRequest("report", "O app travou durante o treino.", "1.0.0", null);
        var response = await _client.PostAsJsonAsync("/api/v1/support/tickets", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<SupportTicketResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Status.Should().Be("open");
    }

    /// <summary>CA-002: Unauthenticated request returns 401.</summary>
    [Fact]
    public async Task UnauthenticatedCannotCreateSupportTicket()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var request = new CreateSupportTicketRequest("question", "Como funciona a loja?", null, null);
        var response = await _client.PostAsJsonAsync("/api/v1/support/tickets", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>CA-003: Invalid category returns 422.</summary>
    [Fact]
    public async Task InvalidCategoryReturnsBadRequest()
    {
        var token = await RegisterAndGetTokenAsync("support_ca003@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateSupportTicketRequest("invalid_category", "Teste.", null, null);
        var response = await _client.PostAsJsonAsync("/api/v1/support/tickets", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>CA-004: Empty description returns 422.</summary>
    [Fact]
    public async Task EmptyDescriptionReturnsBadRequest()
    {
        var token = await RegisterAndGetTokenAsync("support_ca004@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateSupportTicketRequest("suggestion", "", null, null);
        var response = await _client.PostAsJsonAsync("/api/v1/support/tickets", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
