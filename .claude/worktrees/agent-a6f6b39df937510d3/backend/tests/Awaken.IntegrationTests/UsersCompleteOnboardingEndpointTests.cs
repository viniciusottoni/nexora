using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Common;
using Awaken.Contracts.Onboarding;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class UsersCompleteOnboardingEndpointTests : IAsyncLifetime
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

    private async Task<string> RegisterAndGetTokenAsync(string email = "hunter@awaken.app")
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
    }

    private async Task StartTrialAsync()
    {
        var resp = await _client.PostAsync("/api/subscriptions/trial/start", null);
        resp.EnsureSuccessStatusCode();
    }

    private async Task SeedPaidSubscriptionAsync(
        string email, string plan, DateTime expiresAt, string revenueCatCustomerId = "rc_test_customer")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        var now = DateTime.UtcNow;

        var existing = await db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (existing is not null)
        {
            db.Subscriptions.Remove(existing);
            await db.SaveChangesAsync();
        }

        db.Subscriptions.Add(
            Subscription.CreateFromPaidPlan(user.Id, plan, "pro_access", revenueCatCustomerId, expiresAt, now));
        await db.SaveChangesAsync();
    }

    private static object ValidPayload() => new
    {
        goal = "gain_muscle",
        experienceLevel = "beginner",
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

    [Fact]
    public async Task CA001_ReturnsOnboardingCompletedTrue()
    {
        var token = await RegisterAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/users/me/profile/complete-onboarding", ValidPayload());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CompleteOnboardingResponse>();
        body!.OnboardingCompleted.Should().BeTrue();
        body.NextRoute.Should().Be("daily_quest");
        body.Level.Should().Be(1);
    }

    [Fact]
    public async Task CA001_RankScoreNeverExceeds48_AndRankNeverExceedsB_ForMostAdvancedProfile()
    {
        var token = await RegisterAndGetTokenAsync("advancedprofile@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();

        var payload = new
        {
            goal = "gain_muscle",
            experienceLevel = "advanced",
            age = 28,
            heightCm = 175.0,
            weightKg = 82.0,
            biologicalSex = "masculino",
            trainingDuration = "more_than_3_years",
            availableMinutesPerWorkout = 30,
            bodyType = "athletic_strong",
            physicalLimitations = new[] { "no_limitations" },
            physicalPains = new[] { "no_pains" }
        };

        var response = await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", payload);

        var body = await response.Content.ReadFromJsonAsync<CompleteOnboardingResponse>();
        body!.RankScore.Should().Be(48);
        body.Rank.Should().Be("B");
        body.Level.Should().Be(1);
        body.RankCapApplied.Should().BeTrue();
    }

    [Fact]
    public async Task CA001_UserIsMarkedAsOnboardingComplete_InDatabase()
    {
        var token = await RegisterAndGetTokenAsync("dbcheck@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();

        await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", ValidPayload());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == "dbcheck@awaken.app");
        user.IsOnboardingComplete.Should().BeTrue();
        user.OnboardingCompletedAtUtc.Should().NotBeNull();
        user.CurrentOnboardingStep.Should().Be("completed");
    }

    [Fact]
    public async Task CA001_UserProfileIsSavedWithAllFields()
    {
        var token = await RegisterAndGetTokenAsync("profilecheck@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();

        await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", ValidPayload());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var profile = await db.UserProfiles.SingleAsync();
        profile.Goal.Should().Be("gain_muscle");
        profile.ExperienceLevel.Should().Be("beginner");
        profile.Age.Should().Be(28);
        profile.HeightCm.Should().Be(175m);
        profile.WeightKg.Should().Be(82m);
        profile.BiologicalSex.Should().Be("masculino");
        profile.TrainingDuration.Should().Be("1_6_months");
        profile.AvailableMinutesPerWorkout.Should().Be(30);
        profile.BodyType.Should().Be("normal");
        profile.PhysicalLimitations.Should().BeEquivalentTo(new[] { "no_limitations" });
        profile.PhysicalPains.Should().BeEquivalentTo(new[] { "no_pains" });
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync(
            "/api/users/me/profile/complete-onboarding", ValidPayload());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RN002_ReturnsForbidden_WhenSubscriptionExpired()
    {
        var token = await RegisterAndGetTokenAsync("expiredtrial@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await SeedPaidSubscriptionAsync("expiredtrial@awaken.app", "monthly", DateTime.UtcNow.AddDays(-1), "rc_expired_test");

        var response = await _client.PostAsJsonAsync(
            "/api/users/me/profile/complete-onboarding", ValidPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ValidationFails_WhenGoalIsInvalid()
    {
        var token = await RegisterAndGetTokenAsync("invalidgoal@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();

        var payload = new
        {
            goal = "become_hero",
            experienceLevel = "beginner",
            age = 28, heightCm = 175.0, weightKg = 82.0, biologicalSex = "masculino",
            trainingDuration = "1_6_months", availableMinutesPerWorkout = 30,
            bodyType = "normal",
            physicalLimitations = new[] { "no_limitations" },
            physicalPains = new[] { "no_pains" }
        };
        var response = await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", payload);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Code.Should().Be("INVALID_PROFILE_DATA");
        error.Message.Should().Be("Revise os dados informados.");
    }

    [Fact]
    public async Task ValidationFails_WhenAgeIsOutOfRange()
    {
        var token = await RegisterAndGetTokenAsync("invalidage@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();

        var payload = new
        {
            goal = "gain_muscle", experienceLevel = "beginner",
            age = 5, heightCm = 175.0, weightKg = 82.0, biologicalSex = "masculino",
            trainingDuration = "1_6_months", availableMinutesPerWorkout = 30,
            bodyType = "normal",
            physicalLimitations = new[] { "no_limitations" },
            physicalPains = new[] { "no_pains" }
        };
        var response = await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", payload);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ValidationFails_WhenExperienceLevelIsInvalid()
    {
        var token = await RegisterAndGetTokenAsync("invalidlevel@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();

        var payload = new
        {
            goal = "gain_muscle", experienceLevel = "god_tier",
            age = 28, heightCm = 175.0, weightKg = 82.0, biologicalSex = "masculino",
            trainingDuration = "1_6_months", availableMinutesPerWorkout = 30,
            bodyType = "normal",
            physicalLimitations = new[] { "no_limitations" },
            physicalPains = new[] { "no_pains" }
        };
        var response = await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", payload);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task IsIdempotent_CallingTwiceSucceeds()
    {
        var token = await RegisterAndGetTokenAsync("idempotent@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();

        var first = await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", ValidPayload());
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", ValidPayload());
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await second.Content.ReadFromJsonAsync<CompleteOnboardingResponse>();
        body!.OnboardingCompleted.Should().BeTrue();
    }
}
