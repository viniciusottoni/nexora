using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Progression;
using Awaken.Domain.Entities.Progression;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class HunterProgressEndpointTests : IAsyncLifetime
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
        var payload = new
        {
            email,
            password = "Str0ngPass!",
            name = "Hunter",
            language = "pt-BR"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.AccessToken;
    }

    private async Task CreateProgressionAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        var progression = HunterProgression.Create(user.Id);
        progression.AddXp(150, DateTime.UtcNow);
        progression.AddAttributePoints(4, 2, 2, 2, 1, 1, DateTime.UtcNow);
        dbContext.HunterProgressions.Add(progression);

        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetProgressReturnsUnauthorizedWhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/hunter/progress");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProgressReturnsBaselineProgressForNewUser()
    {
        var token = await RegisterAndGetTokenAsync("newprogress@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/hunter/progress");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProgressionResponse>();
        body!.Rank.Should().Be("E");
        body.RankScore.Should().Be(6);
        body.Level.Should().Be(1);
        body.TotalXp.Should().Be(0);
        body.XpToNextLevel.Should().Be(100);
        body.CurrentStreakDays.Should().Be(0);
        body.LongestStreakDays.Should().Be(0);
        body.Attributes.Should().BeEquivalentTo(new AttributesDto(1, 1, 1, 1, 1, 1));
        body.AttributeXp.Should().NotBeNull();
        body.AttributeXp!.Strength.Should().Be(0);
        body.AttributeXp!.Wisdom.Should().Be(0);
    }

    [Fact]
    public async Task GetProgressReturnsCurrentProgressWhenProgressionExists()
    {
        var token = await RegisterAndGetTokenAsync("currentprogress@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await CreateProgressionAsync("currentprogress@awaken.app");

        var response = await _client.GetAsync("/api/hunter/progress");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProgressionResponse>();
        body!.Rank.Should().Be("D");
        body.RankScore.Should().Be(18);
        body.Level.Should().Be(2);
        body.TotalXp.Should().Be(50);
        body.XpToNextLevel.Should().Be(HunterProgression.CalculateXpToNextLevel(2));
        body.Attributes.Should().BeEquivalentTo(new AttributesDto(5, 3, 3, 3, 2, 2));
        body.AttributeXp.Should().NotBeNull();
        body.AttributeXp!.Strength.Should().Be(0);
    }
}
