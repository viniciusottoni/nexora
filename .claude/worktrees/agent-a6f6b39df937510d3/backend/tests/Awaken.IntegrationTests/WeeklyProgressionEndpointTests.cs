using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Quests;
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
/// Integration tests for US-241: GET /api/quests/weekly-progression.
/// </summary>
public class WeeklyProgressionEndpointTests : IAsyncLifetime
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

    private static string UniqueEmail(string prefix) => $"{prefix}_{Guid.NewGuid():N}@awaken.app";

    private async Task<string> RegisterAndGetTokenAsync(string emailPrefix)
    {
        var email = UniqueEmail(emailPrefix);
        var payload = new { email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        return auth.AccessToken;
    }

    private async Task CompleteOnboardingAsync()
    {
        var payload = new
        {
            goal = "gain_muscle",
            experienceLevel = "intermediate",
            age = 28,
            heightCm = 175.0,
            weightKg = 82.0,
            biologicalSex = "masculino",
            trainingDuration = "1_6_months",
            availableMinutesPerWorkout = 30,
            bodyType = "normal",
            physicalLimitations = new[] { "no_limitations" },
            physicalPains = new[] { "no_pains" }
        };

        var response = await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", payload);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetWeeklyProgression_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/quests/weekly-progression");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWeeklyProgression_ForFreshUser_ReturnsHoldDecisionWithinCurrentWeek()
    {
        var token = await RegisterAndGetTokenAsync("weekly_progression_fresh");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await CompleteOnboardingAsync();

        var response = await _client.GetAsync("/api/quests/weekly-progression");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var plan = await response.Content.ReadFromJsonAsync<WeeklyProgressionResponse>();
        plan.Should().NotBeNull();
        plan!.MesocycleWeekIndex.Should().Be(1);
        plan.Decision.Should().Be("hold"); // sem histórico de sentimento ainda
        plan.Rank.Should().NotBeNullOrEmpty(); // rank exato depende do cálculo de onboarding (fora de escopo aqui)
    }

    [Fact]
    public async Task GetWeeklyProgression_CalledTwiceSameWeek_ReturnsSamePlan()
    {
        var token = await RegisterAndGetTokenAsync("weekly_progression_idempotent");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await CompleteOnboardingAsync();

        var first = await (await _client.GetAsync("/api/quests/weekly-progression"))
            .Content.ReadFromJsonAsync<WeeklyProgressionResponse>();
        var second = await (await _client.GetAsync("/api/quests/weekly-progression"))
            .Content.ReadFromJsonAsync<WeeklyProgressionResponse>();

        second.Should().BeEquivalentTo(first);
    }
}
