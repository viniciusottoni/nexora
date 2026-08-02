// US-072/US-121 — RN-001/RN-002/RN-003/RN-004: bloqueio comercial não apaga
// progresso (HunterProgression, UserProfile, Quest), bloqueado não ganha novos
// ganhos e progresso volta a ser exibido após reativação do acesso.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Application.Quests.Common;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Hunter;
using Awaken.Contracts.Users;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;
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

public class ProgressPreservationEndpointTests : IAsyncLifetime
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
        var payload = new { email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
    }

    private async Task<Guid> GetUserIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        return user.Id;
    }

    private async Task SeedHunterProgressionAsync(Guid userId, long xp)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var progression = HunterProgression.Create(userId);
        progression.AddXp(xp, DateTime.UtcNow);
        dbContext.HunterProgressions.Add(progression);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedUserProfileAsync(Guid userId, string goal)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        dbContext.UserProfiles.Add(UserProfile.Create(userId, goal: goal, experienceLevel: "beginner"));
        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid> SeedCompletedQuestAsync(Guid userId, long xpAwarded)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var quest = Quest.Create(userId, DateTime.UtcNow.Date, "pt-BR", $"idem-{Guid.NewGuid()}");
        quest.Complete(xpAwarded, DateTime.UtcNow);
        dbContext.Quests.Add(quest);
        await dbContext.SaveChangesAsync();
        return quest.Id;
    }

    private async Task ExpireTrialAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        var subscription = await dbContext.Subscriptions.SingleAsync(s => s.UserId == user.Id);

        var trialEndsAt = DateTime.UtcNow.AddDays(-1);
        dbContext.Entry(user).Property(nameof(User.TrialEndsAt)).CurrentValue = trialEndsAt;
        dbContext.Entry(subscription).Property(nameof(Subscription.TrialEndsAt)).CurrentValue = trialEndsAt;

        await dbContext.SaveChangesAsync();
        await InvalidateAccessCacheAsync(user.Id);
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
        await InvalidateAccessCacheAsync(user.Id);
    }

    private async Task InvalidateAccessCacheAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IAccessStatusCacheService>();
        await cache.InvalidateAsync(userId);
    }

    private static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@awaken.app";

    [Fact]
    public async Task HunterProgressionXpSurvivesTrialExpirationAndIsRestoredAfterAccessReturns()
    {
        var email = UniqueEmail("progress_xp");
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var userId = await GetUserIdAsync(email);
        await SeedHunterProgressionAsync(userId, xp: 250);
        await _client.PostAsync("/api/subscriptions/trial/start", null);

        var beforeBlock = await _client.GetAsync("/api/hunter/profile");
        beforeBlock.StatusCode.Should().Be(HttpStatusCode.OK);
        // API expõe XP corrente do nível, não XP acumulado bruto.
        (await beforeBlock.Content.ReadFromJsonAsync<HunterProfileResponse>())!.Xp.Should().Be(150);

        await ExpireTrialAsync(email);

        var whileBlocked = await _client.GetAsync("/api/hunter/profile");
        whileBlocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var progression = await dbContext.HunterProgressions.SingleAsync(p => p.UserId == userId);
            // TotalXp no DB = XP do nível atual: AddXp(250) triggou level-up (100→L2), TotalXp fica em 150.
            progression.TotalXp.Should().Be(150);
        }

        await SeedPaidSubscriptionAsync(email, "monthly", DateTime.UtcNow.AddDays(30), "rc_progress_xp_test");

        var afterRestore = await _client.GetAsync("/api/hunter/profile");
        afterRestore.StatusCode.Should().Be(HttpStatusCode.OK);
        var restoredProfile = await afterRestore.Content.ReadFromJsonAsync<HunterProfileResponse>();
        restoredProfile!.HasProgress.Should().BeTrue();
        restoredProfile.Xp.Should().Be(150);
    }

    [Fact]
    public async Task UserProfileSurvivesSubscriptionExpirationAndIsRestoredAfterAccessReturns()
    {
        var email = UniqueEmail("progress_profile");
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var userId = await GetUserIdAsync(email);
        await SeedUserProfileAsync(userId, goal: "gain_muscle");

        await SeedPaidSubscriptionAsync(email, "monthly", DateTime.UtcNow.AddDays(-1), "rc_progress_profile_test");

        var whileBlocked = await _client.GetAsync("/api/users/me/profile");
        whileBlocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var profile = await dbContext.UserProfiles.SingleAsync(p => p.UserId == userId);
            profile.Goal.Should().Be("gain_muscle");
        }

        await SeedPaidSubscriptionAsync(email, "monthly", DateTime.UtcNow.AddDays(30), "rc_progress_profile_test");

        var afterRestore = await _client.GetAsync("/api/users/me/profile");
        afterRestore.StatusCode.Should().Be(HttpStatusCode.OK);
        var restoredProfile = await afterRestore.Content.ReadFromJsonAsync<UserProfileResponse>();
        restoredProfile!.Goal.Should().Be("gain_muscle");
    }

    [Fact]
    public async Task CompletedQuestHistoryIsNotDeletedWhenAccessIsBlocked()
    {
        var email = UniqueEmail("progress_quest");
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var userId = await GetUserIdAsync(email);
        var questId = await SeedCompletedQuestAsync(userId, xpAwarded: 50);

        await SeedPaidSubscriptionAsync(email, "monthly", DateTime.UtcNow.AddDays(-1), "rc_progress_quest_test");

        var blockedResponse = await _client.GetAsync("/api/users/me/profile");
        blockedResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var quest = await dbContext.Quests.SingleAsync(q => q.Id == questId);
        quest.Status.Should().Be("completed");
        quest.XpAwarded.Should().Be(50);
        quest.UserId.Should().Be(userId);
    }

    // US-072 / RN-001: Level, Rank, RankScore, todos os atributos e Streak são preservados após bloqueio.
    [Fact]
    public async Task RN001_AllProgressionFields_ArePersisted_AfterTrialExpiry()
    {
        var email = UniqueEmail("progress_allfields");
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var userId = await GetUserIdAsync(email);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var prog = HunterProgression.CreateFromOnboarding(
                userId, strength: 5, agility: 4, endurance: 3, vitality: 4, focus: 3, wisdom: 2);
            // RankScore = 21 → Rank D. XP acumulado + streak simulado.
            prog.AddXp(350, DateTime.UtcNow);
            prog.UpdateStreakAfterQuestCompletion(DateTime.UtcNow.AddDays(-1));
            prog.UpdateStreakAfterQuestCompletion(DateTime.UtcNow);
            dbContext.HunterProgressions.Add(prog);
            await dbContext.SaveChangesAsync();
        }

        await _client.PostAsync("/api/subscriptions/trial/start", null);
        await ExpireTrialAsync(email);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var saved = await verifyDb.HunterProgressions.AsNoTracking().SingleAsync(p => p.UserId == userId);

        saved.Strength.Should().Be(5);
        saved.Agility.Should().Be(4);
        saved.Endurance.Should().Be(3);
        saved.Vitality.Should().Be(4);
        saved.Focus.Should().Be(3);
        saved.Wisdom.Should().Be(2);
        saved.RankScore.Should().Be(21);
        saved.Rank.Should().Be("D");
        saved.Level.Should().BeGreaterThan(1);
        saved.CurrentStreakDays.Should().Be(2);
    }

    // US-072 / RN-002: usuário bloqueado não consegue completar exercício (403) e XP não muda.
    [Fact]
    public async Task RN002_BlockedUser_CannotCompleteExercise_XpUnchanged()
    {
        var email = UniqueEmail("progress_block_xp");
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var userId = await GetUserIdAsync(email);
        await SeedHunterProgressionAsync(userId, xp: 50);
        await _client.PostAsync("/api/subscriptions/trial/start", null);

        // Cria e inicia quest diretamente no DB (antes de bloquear).
        const string WorkoutJson = """
            {
              "title": "Daily Quest",
              "exercises": [
                { "id": "ex-block", "name": "Squat", "sets": 3, "repsMin": 8, "repsMax": 12, "restSeconds": 60, "targetRpe": "8",
                  "attributeContribution": { "strengthXp": 10, "agilityXp": 0, "enduranceXp": 0, "vitalityXp": 0, "focusXp": 0, "wisdomXp": 1 } }
              ]
            }
            """;

        Guid questId;
        Guid exerciseId;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var quest = Quest.Create(userId, DateTime.UtcNow.Date, "pt-BR", $"block-test-{Guid.NewGuid():N}");
            quest.AssignWorkout(WorkoutJson, DateTime.UtcNow);
            quest.Start(DateTime.UtcNow, QuestExerciseSeedMapper.ParseSeeds(WorkoutJson));
            dbContext.Quests.Add(quest);
            await dbContext.SaveChangesAsync();
            questId = quest.Id;
            exerciseId = quest.Exercises.First().Id;
        }

        await ExpireTrialAsync(email);

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/exercises/{exerciseId}/complete",
            new { setsCompleted = 3, strongPainReported = false });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var prog = await verifyDb.HunterProgressions.AsNoTracking().SingleAsync(p => p.UserId == userId);
        prog.TotalXp.Should().Be(50);
    }

    // US-072 / RN-004: streak não avança durante o bloqueio e é recuperado ao reativar.
    [Fact]
    public async Task RN004_Streak_IsPreservedDuringBlock_AndNotAdvanced()
    {
        var email = UniqueEmail("progress_streak");
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var userId = await GetUserIdAsync(email);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var prog = HunterProgression.Create(userId);
            prog.UpdateStreakAfterQuestCompletion(DateTime.UtcNow.AddDays(-2));
            prog.UpdateStreakAfterQuestCompletion(DateTime.UtcNow.AddDays(-1));
            dbContext.HunterProgressions.Add(prog);
            await dbContext.SaveChangesAsync();
        }

        await SeedPaidSubscriptionAsync(email, "monthly", DateTime.UtcNow.AddDays(-1), "rc_streak_test");

        // Durante bloqueio não é possível completar quests, portanto streak não avança.
        // Verificamos que o streak armazenado permanece o mesmo após o bloqueio.
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var saved = await verifyDb.HunterProgressions.AsNoTracking().SingleAsync(p => p.UserId == userId);
        saved.CurrentStreakDays.Should().Be(2);

        // Reativa: progresso deve estar intacto.
        await SeedPaidSubscriptionAsync(email, "monthly", DateTime.UtcNow.AddDays(30), "rc_streak_test");
        var afterRestore = await _client.GetAsync("/api/hunter/profile");
        afterRestore.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await afterRestore.Content.ReadFromJsonAsync<HunterProfileResponse>();
        profile!.HasProgress.Should().BeTrue();
    }
}
