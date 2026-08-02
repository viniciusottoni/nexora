using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Common;
using Awaken.Contracts.Users;
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

public class UsersUpdateProfileEndpointTests : IAsyncLifetime
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

    private async Task<string> RegisterAndGetTokenAsync(string email)
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

    private static object CompleteOnboardingPayload() => new
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

    private async Task CompleteOnboardingAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/users/me/profile/complete-onboarding", CompleteOnboardingPayload());
        response.EnsureSuccessStatusCode();
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

        // US-205 RN-005: ActiveAccessMiddleware cacheia o access status por 60s
        // no Redis. Mutar a subscription direto no banco (fora do fluxo normal
        // de comando) nao invalida esse cache sozinho - sem isto, uma chamada
        // autenticada anterior nesta mesma janela de 60s (ex.: CompleteOnboardingAsync)
        // deixaria o status antigo em cache e o teste veria a requisicao seguinte
        // liberada em vez de bloqueada.
        var cache = scope.ServiceProvider.GetRequiredService<IAccessStatusCacheService>();
        await cache.InvalidateAsync(user.Id);
    }

    private static object UpdatePayload() => new
    {
        goal = "gain_strength",
        trainingLocation = "gym",
        equipmentAvailable = new[] { "dumbbells", "full_gym" },
        availableMinutesPerWorkout = 40,
        availableDaysPerWeek = 4,
        physicalLimitations = new[] { "knee_problem" },
        trainingPreferences = new[] { "low_impact", "strength_focus" }
    };

    [Fact]
    public async Task CA001_UpdatesAndPersistsProfile()
    {
        var token = await RegisterAndGetTokenAsync("editprofile@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();

        var response = await _client.PutAsJsonAsync("/api/users/me/profile", UpdatePayload());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        body!.Goal.Should().Be("gain_strength");
        body.TrainingLocation.Should().Be("gym");
        body.EquipmentAvailable.Should().BeEquivalentTo(new[] { "dumbbells", "full_gym" });
        body.AvailableMinutesPerWorkout.Should().Be(40);
        body.AvailableDaysPerWeek.Should().Be(4);
        body.PhysicalLimitations.Should().BeEquivalentTo(new[] { "knee_problem" });
        body.TrainingPreferences.Should().BeEquivalentTo(new[] { "low_impact", "strength_focus" });
    }

    [Fact]
    public async Task CA001_GetReflectsUpdatedValues()
    {
        var token = await RegisterAndGetTokenAsync("getafterupdate@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();
        await _client.PutAsJsonAsync("/api/users/me/profile", UpdatePayload());

        var response = await _client.GetAsync("/api/users/me/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        body!.Goal.Should().Be("gain_strength");
        body.ExperienceLevel.Should().Be("beginner");
    }

    [Fact]
    public async Task CA003_UnrelatedFieldsRemainAfterUpdate()
    {
        var token = await RegisterAndGetTokenAsync("preserve@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();

        await _client.PutAsJsonAsync("/api/users/me/profile", UpdatePayload());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == "preserve@awaken.app");
        user.IsOnboardingComplete.Should().BeTrue();
        user.OnboardingCompletedAtUtc.Should().NotBeNull();

        var profile = await db.UserProfiles.SingleAsync(p => p.UserId == user.Id);
        profile.Age.Should().Be(28);
        profile.BodyType.Should().Be("normal");
    }

    [Fact]
    public async Task RN002_ReturnsUnprocessableEntity_WhenGoalIsEmpty()
    {
        var token = await RegisterAndGetTokenAsync("emptygoal@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();

        var response = await _client.PutAsJsonAsync("/api/users/me/profile", new { goal = "" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Code.Should().Be("INVALID_PROFILE_DATA");
        error.Message.Should().Be("Revise os dados informados.");
    }

    [Fact]
    public async Task RN006_ReturnsForbidden_WhenSubscriptionExpired()
    {
        var token = await RegisterAndGetTokenAsync("expiredprofile@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();

        await SeedPaidSubscriptionAsync("expiredprofile@awaken.app", "monthly", DateTime.UtcNow.AddDays(-1), "rc_expired_test");

        var response = await _client.PutAsJsonAsync("/api/users/me/profile", UpdatePayload());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenProfileDoesNotExistYet()
    {
        var token = await RegisterAndGetTokenAsync("noprofile@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();

        var response = await _client.PutAsJsonAsync("/api/users/me/profile", UpdatePayload());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PutAsJsonAsync("/api/users/me/profile", UpdatePayload());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
