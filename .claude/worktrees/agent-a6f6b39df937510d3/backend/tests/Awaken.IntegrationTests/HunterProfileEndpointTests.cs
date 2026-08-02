using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Hunter;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

public class HunterProfileEndpointTests : IAsyncLifetime
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
            // HYPOTHESIS TEST: UseSetting loses to Program.cs's
            // AddJsonFile("appsettings.Local.json") on this dev machine.
            // ConfigureAppConfiguration should be applied after Program.cs's
            // own configuration setup runs, so it should win instead.
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

    private async Task CreateProgressionAsync(string email, long xp = 0)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        var existingProgression = await dbContext.HunterProgressions
            .FirstOrDefaultAsync(p => p.UserId == user.Id);
        var progression = existingProgression ?? HunterProgression.Create(user.Id);
        if (xp > 0)
        {
            progression.AddXp(xp, DateTime.UtcNow);
        }

        if (existingProgression is null)
        {
            dbContext.HunterProgressions.Add(progression);
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task SetRecentDailyPenaltyAsync(
        string email,
        long penaltyXp,
        DateTime? questDateUtc = null)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        var progressions = await dbContext.HunterProgressions
            .Where(p => p.UserId == user.Id)
            .ToListAsync();

        if (progressions.Count == 0)
        {
            var progression = HunterProgression.Create(user.Id);
            dbContext.HunterProgressions.Add(progression);
            progressions.Add(progression);
        }

        foreach (var progression in progressions)
        {
            dbContext.Entry(progression).Property(nameof(HunterProgression.RecentDailyPenaltyXp))
                .CurrentValue = penaltyXp;
            dbContext.Entry(progression).Property(nameof(HunterProgression.RecentDailyPenaltyQuestDateUtc))
                .CurrentValue = questDateUtc;
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task SetAvatarAsync(string email, string avatarUrl)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        user.UpdateProfile(user.DisplayName, avatarUrl, DateTime.UtcNow);
        await dbContext.SaveChangesAsync();
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

    [Fact]
    public async Task GetProfileReturnsUnauthorizedWhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/hunter/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfileReturnsNoTrialAndNoProgressWhenUserJustRegistered()
    {
        var token = await RegisterAndGetTokenAsync("noprogress@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/hunter/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HunterProfileResponse>();
        body!.AccessStatus.Should().Be("no_trial");
        body.HasProgress.Should().BeFalse();
        body.DisplayName.Should().Be("Hunter");
        body.AvatarUrl.Should().BeNull();
        body.HunterClass.Should().Be("beginner_hunter");
        body.Rank.Should().BeNull();
        body.Attributes.Should().BeNull();
    }

    [Fact]
    public async Task GetProfileReturnsAvatarUrlWhenUserHasAvatar()
    {
        var token = await RegisterAndGetTokenAsync("avatarprofile@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await SetAvatarAsync("avatarprofile@awaken.app", "https://cdn.awaken.app/avatar.png");

        var response = await _client.GetAsync("/api/hunter/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HunterProfileResponse>();
        body!.AvatarUrl.Should().Be("https://cdn.awaken.app/avatar.png");
    }

    [Fact]
    public async Task GetProfileReturnsTrialActiveAfterStartingTrial()
    {
        var token = await RegisterAndGetTokenAsync("trialprofile@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsync("/api/subscriptions/trial/start", null);
        await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", new
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
        });
        await CreateProgressionAsync("trialprofile@awaken.app");

        var response = await _client.GetAsync("/api/hunter/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HunterProfileResponse>();
        body!.AccessStatus.Should().Be("trial_active");
        body.CardVariant.Should().Be("trial");
        body.HasProgress.Should().BeTrue();
        body.HunterClass.Should().Be("beginner_hunter");
        body.Attributes.Should().NotBeNull();
        body.Attributes!.Wisdom.Should().Be(3);
        body.AttributeXp.Should().NotBeNull();
        body.AttributeXp!.Strength.Should().Be(0);
        body.AttributeXp!.Wisdom.Should().Be(0);
    }

    [Fact]
    public async Task GetProfileReturnsActualRecentDailyPenaltyXpWhenAvailable()
    {
        var token = await RegisterAndGetTokenAsync("penaltyprofile@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsync("/api/subscriptions/trial/start", null);
        await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", new
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
        });

        var questDateUtc = new DateTime(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc);
        await SetRecentDailyPenaltyAsync("penaltyprofile@awaken.app", 5, questDateUtc);

        var response = await _client.GetAsync("/api/hunter/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HunterProfileResponse>();
        body!.RecentDailyPenaltyXp.Should().Be(5);
        body.RecentDailyPenaltyQuestDateUtc.Should().Be(questDateUtc);
    }

    [Fact]
    public async Task GetProfileReturnsSubscriptionActiveAfterSync()
    {
        var token = await RegisterAndGetTokenAsync("subprofile@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(30);
        await SeedPaidSubscriptionAsync("subprofile@awaken.app", "monthly", expiresAt, "rc_customer_hunter_test");
        await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", new
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
        });
        await CreateProgressionAsync("subprofile@awaken.app");

        var response = await _client.GetAsync("/api/hunter/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HunterProfileResponse>();
        body!.AccessStatus.Should().Be("subscription_active");
        body.CardVariant.Should().Be("premium");
        body.HasProgress.Should().BeTrue();
        body.HunterClass.Should().Be("beginner_hunter");
        body.Attributes.Should().NotBeNull();
        body.Attributes!.Wisdom.Should().Be(3);
        body.AttributeXp.Should().NotBeNull();
        body.AttributeXp!.Strength.Should().Be(0);
    }

    // US-235 RN-001: apenas assinatura anual paga e ATIVA libera a moldura
    // dourada exposta no perfil do Hunter.
    [Fact]
    public async Task GetProfileReturnsHasAnnualGoldenFrameTrueWhenAnnualSubscriptionActive()
    {
        var token = await RegisterAndGetTokenAsync("annualgoldenframe@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(365);
        await SeedPaidSubscriptionAsync(
            "annualgoldenframe@awaken.app", "annual", expiresAt, "rc_customer_annual_test");
        await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", new
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
        });
        await CreateProgressionAsync("annualgoldenframe@awaken.app");

        var response = await _client.GetAsync("/api/hunter/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HunterProfileResponse>();
        body!.HasAnnualGoldenFrame.Should().BeTrue();
    }

    // US-235 RN-002: plano mensal ativo nunca libera a moldura dourada.
    [Fact]
    public async Task GetProfileReturnsHasAnnualGoldenFrameFalseWhenMonthlySubscriptionActive()
    {
        var token = await RegisterAndGetTokenAsync("monthlygoldenframe@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(30);
        await SeedPaidSubscriptionAsync(
            "monthlygoldenframe@awaken.app", "monthly", expiresAt, "rc_customer_monthly_test");
        await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", new
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
        });
        await CreateProgressionAsync("monthlygoldenframe@awaken.app");

        var response = await _client.GetAsync("/api/hunter/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HunterProfileResponse>();
        body!.HasAnnualGoldenFrame.Should().BeFalse();
    }

    [Fact]
    public async Task GetProfileReturnsStandardCardVariantWhenPremiumFeatureDisabled()
    {
        var token = await RegisterAndGetTokenAsync("disabledpremium@awaken.app");

        var expiresAt = DateTime.UtcNow.AddDays(30);
        await SeedPaidSubscriptionAsync("disabledpremium@awaken.app", "monthly", expiresAt, "rc_customer_disabled_test");
        using var disabledFactory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Features:PremiumCardEnabled", "false"));
        using var disabledClient = disabledFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
        disabledClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await disabledClient.GetAsync("/api/hunter/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HunterProfileResponse>();
        body!.AccessStatus.Should().Be("subscription_active");
        body.CardVariant.Should().Be("standard");
    }

    [Fact]
    public async Task GetCardDataReturnsTheSameSafeProfileShape()
    {
        var token = await RegisterAndGetTokenAsync("carddata@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsync("/api/subscriptions/trial/start", null);
        await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", new
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
        });
        await CreateProgressionAsync("carddata@awaken.app");

        var response = await _client.GetAsync("/api/hunter/card-data");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HunterProfileResponse>();
        body!.AccessStatus.Should().Be("trial_active");
        body.CardVariant.Should().Be("trial");
        body.HasProgress.Should().BeTrue();
        body.DisplayName.Should().Be("Hunter");
        body.HunterClass.Should().Be("beginner_hunter");
        body.Rank.Should().Be("D");
        body.Level.Should().Be(1);
        body.Xp.Should().Be(0);
        body.XpToNextLevel.Should().Be(100);
        body.StreakDays.Should().Be(0);
        body.Attributes.Should().NotBeNull();
        body.Attributes!.Strength.Should().Be(3);
        body.Attributes!.Endurance.Should().Be(3);
        body.Attributes!.Agility.Should().Be(3);
        body.Attributes!.Vitality.Should().Be(3);
        body.Attributes!.Focus.Should().Be(3);
        body.Attributes!.Wisdom.Should().Be(3);
    }
}
