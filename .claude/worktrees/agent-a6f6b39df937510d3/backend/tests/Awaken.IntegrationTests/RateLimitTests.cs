using System.Net;
using System.Net.Http.Json;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class RateLimitTests : IAsyncLifetime
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
    public async Task AuthEndpointReturns429AfterExceedingRateLimit()
    {
        // The "auth" limiter allows 10 requests per minute.
        // Send 11 login attempts. The 11th should return 429.
        var responses = new List<HttpResponseMessage>();

        for (int i = 0; i < 11; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                email = $"ratelimit-test-{i}@awaken.app",
                password = "SomePass1!"
            });
            responses.Add(response);
        }

        // The first 10 should succeed or fail with auth errors (not 429)
        responses.Take(10).Should().AllSatisfy(r =>
            r.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests));

        // The 11th should be rate-limited
        responses[10].StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
