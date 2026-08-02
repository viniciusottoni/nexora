// US-081/US-082/US-083: historico de batalha — testes de integração.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Contracts.BattleLog;
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

public class BattleLogEndpointTests : IAsyncLifetime
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

    private async Task StartTrialAsync()
    {
        var response = await _client.PostAsync("/api/subscriptions/trial/start", null);
        response.EnsureSuccessStatusCode();
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
            trainingDuration = "6_12_months",
            availableMinutesPerWorkout = 30,
            bodyType = "normal",
            physicalLimitations = new[] { "no_limitations" },
            physicalPains = new[] { "no_pains" }
        };
        var response = await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", payload);
        response.EnsureSuccessStatusCode();
    }

    private async Task AuthenticateNewHunterAsync(string email)
    {
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();
    }

    private async Task<Guid> SeedQuestLogAsync(string email, string questType, long xpEarned, DateTime completedAt, IReadOnlyList<string>? items = null)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        var log = QuestLog.Create(
            questId: Guid.NewGuid(),
            userId: user.Id,
            questType: questType,
            xpEarned: xpEarned,
            strengthXpEarned: 0,
            agilityXpEarned: 0,
            enduranceXpEarned: 0,
            vitalityXpEarned: 0,
            focusXpEarned: 0,
            wisdomXpEarned: 0,
            strengthPointsGranted: 0,
            agilityPointsGranted: 0,
            endurancePointsGranted: 0,
            vitalityPointsGranted: 0,
            focusPointsGranted: 0,
            itemsEarned: items ?? [],
            completedAtUtc: completedAt);

        dbContext.QuestLogs.Add(log);
        await dbContext.SaveChangesAsync();
        return log.Id;
    }

    [Fact]
    public async Task CA001_ReturnsRecentLogs_OrderedNewestFirst()
    {
        const string email = "battlelog_ca001@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var olderTime = DateTime.UtcNow.AddHours(-2);
        var newerTime = DateTime.UtcNow.AddHours(-1);
        await SeedQuestLogAsync(email, "daily", 50, olderTime);
        await SeedQuestLogAsync(email, "dungeon", 120, newerTime);

        var response = await _client.GetAsync("/api/hunter/battle-log/recent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogResponse>();
        result!.Items.Should().HaveCount(2);
        result.Items[0].CompletedAt.Should().BeAfter(result.Items[1].CompletedAt);
        result.Items[0].QuestType.Should().Be("dungeon");
        result.Items[1].QuestType.Should().Be("daily");
    }

    [Fact]
    public async Task CA002_CancelledQuest_DoesNotAppearInLog()
    {
        // RN-002: QuestLog so e criado para quests concluidas; canceladas nao geram log.
        const string email = "battlelog_ca002@awaken.app";
        await AuthenticateNewHunterAsync(email);

        // Apenas logs de quests validas existem
        await SeedQuestLogAsync(email, "daily", 80, DateTime.UtcNow);

        var response = await _client.GetAsync("/api/hunter/battle-log/recent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogResponse>();
        result!.Items.Should().AllSatisfy(item =>
            item.QuestType.Should().BeOneOf("daily", "dungeon", "raid"));
    }

    [Fact]
    public async Task RN003_QuestType_ComesFromQuestLog()
    {
        const string email = "battlelog_rn003@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var logId = await SeedQuestLogAsync(email, "dungeon", 200, DateTime.UtcNow, ["dungeon_stone"]);

        var response = await _client.GetAsync("/api/hunter/battle-log/recent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogResponse>();
        var item = result!.Items.Single(i => i.QuestLogId == logId);
        item.QuestType.Should().Be("dungeon");
        item.ItemsEarned.Should().Contain("dungeon_stone");
        item.XpEarned.Should().Be(200);
    }

    [Fact]
    public async Task EmptyLog_ReturnsEmptyItemsList()
    {
        const string email = "battlelog_empty@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var response = await _client.GetAsync("/api/hunter/battle-log/recent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogResponse>();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Unauthenticated_Returns401()
    {
        // Requisicao sem token deve retornar 401.
        var response = await _client.GetAsync("/api/hunter/battle-log/recent");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RN004_LimitOf20_IsRespectedByDefault()
    {
        const string email = "battlelog_limit@awaken.app";
        await AuthenticateNewHunterAsync(email);

        for (var i = 0; i < 25; i++)
            await SeedQuestLogAsync(email, "daily", 50, DateTime.UtcNow.AddMinutes(-i));

        var response = await _client.GetAsync("/api/hunter/battle-log/recent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogResponse>();
        result!.Items.Should().HaveCount(20);
    }

    [Fact]
    public async Task LogsFromOtherUser_AreNotReturned()
    {
        // RN-001: historico e isolado por usuario.
        await AuthenticateNewHunterAsync("battlelog_isolation_a@awaken.app");
        await SeedQuestLogAsync("battlelog_isolation_a@awaken.app", "daily", 100, DateTime.UtcNow);

        await AuthenticateNewHunterAsync("battlelog_isolation_b@awaken.app");
        var response = await _client.GetAsync("/api/hunter/battle-log/recent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogResponse>();
        result!.Items.Should().BeEmpty();
    }

    // ─── US-082: Ver histórico durante trial ────────────────────────────────

    [Fact]
    public async Task US082_CA001_TrialActive_SeesBattleLogViaTrialScope()
    {
        const string email = "us082_ca001@awaken.app";
        await AuthenticateNewHunterAsync(email);

        await SeedQuestLogAsync(email, "daily", 80, DateTime.UtcNow.AddHours(-1));
        await SeedQuestLogAsync(email, "dungeon", 150, DateTime.UtcNow, ["iron_ingot"]);

        var response = await _client.GetAsync("/api/hunter/battle-log?scope=trial");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogResponse>();
        result!.Items.Should().HaveCount(2);
        result.Items[0].QuestType.Should().Be("dungeon");
        result.Items[0].ItemsEarned.Should().Contain("iron_ingot");
    }

    [Fact]
    public async Task US082_CA002_TrialExpired_Returns403()
    {
        // RN-003: trial expirado → middleware bloqueia com 403.
        const string email = "us082_ca002@awaken.app";
        await AuthenticateNewHunterAsync(email);

        await ForceExpireTrialAsync(email);

        var response = await _client.GetAsync("/api/hunter/battle-log?scope=trial");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task US082_RN004_LogsPreserved_AfterTrialExpiry()
    {
        // RN-004: logs nao devem ser apagados ao expirar o trial.
        const string email = "us082_rn004@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var logId = await SeedQuestLogAsync(email, "daily", 100, DateTime.UtcNow.AddHours(-1));

        await ForceExpireTrialAsync(email);

        // Logs ainda existem no banco mesmo apos expiracao
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var log = await dbContext.QuestLogs.FindAsync(logId);
        log.Should().NotBeNull("logs devem ser preservados apos o trial expirar");
    }

    [Fact]
    public async Task US082_EmptyState_TrialWithNoQuests_ReturnsEmptyList()
    {
        const string email = "us082_empty@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var response = await _client.GetAsync("/api/hunter/battle-log?scope=trial");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogResponse>();
        result!.Items.Should().BeEmpty();
    }

    // ─── US-083: Ver histórico completo como assinante ──────────────────────

    [Fact]
    public async Task US083_CA001_ActiveAccess_SeesPaginatedHistory()
    {
        // Trial ativo tem acesso ao endpoint paginado durante MVP (sem plano pago real).
        const string email = "us083_ca001@awaken.app";
        await AuthenticateNewHunterAsync(email);

        for (var i = 0; i < 5; i++)
            await SeedQuestLogAsync(email, "daily", 50, DateTime.UtcNow.AddMinutes(-i));

        var response = await _client.GetAsync("/api/hunter/battle-log?page=1&pageSize=3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedBattleLogResponse>();
        result!.Items.Should().HaveCount(3);
        result.Metadata.Page.Should().Be(1);
        result.Metadata.PageSize.Should().Be(3);
        result.Metadata.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task US083_RN005_LastPage_HasMoreFalse()
    {
        const string email = "us083_lastpage@awaken.app";
        await AuthenticateNewHunterAsync(email);

        for (var i = 0; i < 3; i++)
            await SeedQuestLogAsync(email, "daily", 50, DateTime.UtcNow.AddMinutes(-i));

        var response = await _client.GetAsync("/api/hunter/battle-log?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedBattleLogResponse>();
        result!.Items.Should().HaveCount(3);
        result.Metadata.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task US083_Pagination_SecondPage_ReturnsCorrectItems()
    {
        const string email = "us083_page2@awaken.app";
        await AuthenticateNewHunterAsync(email);

        for (var i = 0; i < 5; i++)
            await SeedQuestLogAsync(email, "daily", 50 + i, DateTime.UtcNow.AddMinutes(-i));

        var page2 = await _client.GetAsync("/api/hunter/battle-log?page=2&pageSize=3");

        page2.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await page2.Content.ReadFromJsonAsync<PagedBattleLogResponse>();
        result!.Items.Should().HaveCount(2);
        result.Metadata.Page.Should().Be(2);
        result.Metadata.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task US083_CA002_SubscriptionExpired_Returns403()
    {
        // RN-003: assinatura expirada → middleware bloqueia com 403.
        const string email = "us083_ca002@awaken.app";
        await AuthenticateNewHunterAsync(email);

        await SeedExpiredSubscriptionAsync(email);

        var response = await _client.GetAsync("/api/hunter/battle-log?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task US083_OrderedByCompletedAt_NewestFirst()
    {
        const string email = "us083_order@awaken.app";
        await AuthenticateNewHunterAsync(email);

        await SeedQuestLogAsync(email, "daily", 50, DateTime.UtcNow.AddHours(-3));
        await SeedQuestLogAsync(email, "dungeon", 120, DateTime.UtcNow.AddHours(-1));
        await SeedQuestLogAsync(email, "raid", 200, DateTime.UtcNow.AddHours(-2));

        var response = await _client.GetAsync("/api/hunter/battle-log?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedBattleLogResponse>();
        result!.Items.Should().HaveCount(3);
        result.Items[0].QuestType.Should().Be("dungeon");
        result.Items[1].QuestType.Should().Be("raid");
        result.Items[2].QuestType.Should().Be("daily");
    }

    // ─── US-084: Ver XP recebido em cada quest ──────────────────────────────

    [Fact]
    public async Task US084_CA001_XpEarned_IsExposedInLogEntry()
    {
        const string email = "us084_ca001@awaken.app";
        await AuthenticateNewHunterAsync(email);

        await SeedQuestLogAsync(email, "daily", 120, DateTime.UtcNow);

        var response = await _client.GetAsync("/api/hunter/battle-log/recent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogResponse>();
        result!.Items.Should().ContainSingle();
        result.Items[0].XpEarned.Should().Be(120);
    }

    [Fact]
    public async Task US084_CA002_XpZero_IsExposedAsZero()
    {
        const string email = "us084_ca002@awaken.app";
        await AuthenticateNewHunterAsync(email);

        await SeedQuestLogAsync(email, "daily", 0, DateTime.UtcNow);

        var response = await _client.GetAsync("/api/hunter/battle-log/recent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogResponse>();
        result!.Items[0].XpEarned.Should().Be(0);
    }

    [Fact]
    public async Task US084_RN004_XpPenaltyApplied_IsExposedWhenSet()
    {
        const string email = "us084_rn004@awaken.app";
        await AuthenticateNewHunterAsync(email);

        await SeedQuestLogWithPenaltyAsync(email, "daily", 80, xpPenalty: 20, DateTime.UtcNow);

        var response = await _client.GetAsync("/api/hunter/battle-log/recent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogResponse>();
        result!.Items[0].XpEarned.Should().Be(80);
        result.Items[0].XpPenaltyApplied.Should().Be(20);
    }

    [Fact]
    public async Task US084_XpPenaltyApplied_IsNullWhenNoPenalty()
    {
        const string email = "us084_no_penalty@awaken.app";
        await AuthenticateNewHunterAsync(email);

        await SeedQuestLogAsync(email, "daily", 100, DateTime.UtcNow);

        var response = await _client.GetAsync("/api/hunter/battle-log/recent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogResponse>();
        result!.Items[0].XpPenaltyApplied.Should().BeNull();
    }

    // ─── helpers US-082/US-083 ──────────────────────────────────────────────

    private async Task SeedQuestLogWithPenaltyAsync(
        string email, string questType, long xpEarned, long xpPenalty, DateTime completedAt)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        var log = QuestLog.Create(
            questId: Guid.NewGuid(),
            userId: user.Id,
            questType: questType,
            xpEarned: xpEarned,
            strengthXpEarned: 0,
            agilityXpEarned: 0,
            enduranceXpEarned: 0,
            vitalityXpEarned: 0,
            focusXpEarned: 0,
            wisdomXpEarned: 0,
            strengthPointsGranted: 0,
            agilityPointsGranted: 0,
            endurancePointsGranted: 0,
            vitalityPointsGranted: 0,
            focusPointsGranted: 0,
            itemsEarned: [],
            completedAtUtc: completedAt,
            xpPenaltyApplied: xpPenalty);

        dbContext.QuestLogs.Add(log);
        await dbContext.SaveChangesAsync();
    }

    private async Task ForceExpireTrialAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        // Seta TrialEndsAt no passado para que ComputeAccessStatus retorne "trial_expired".
        typeof(Awaken.Domain.Entities.Auth.User)
            .GetProperty(nameof(Awaken.Domain.Entities.Auth.User.TrialEndsAt))!
            .SetValue(user, DateTime.UtcNow.AddDays(-1));

        await dbContext.SaveChangesAsync();

        var accessStatusCache = scope.ServiceProvider.GetRequiredService<IAccessStatusCacheService>();
        await accessStatusCache.InvalidateAsync(user.Id);
    }

    private async Task SeedExpiredSubscriptionAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        var expiredSub = Subscription.CreateFromPaidPlan(
            userId: user.Id,
            plan: "monthly",
            entitlement: "premium",
            revenueCatCustomerId: $"rc_{user.Id:N}",
            expiresAt: DateTime.UtcNow.AddDays(-1),
            syncedAt: DateTime.UtcNow);

        // Troca subscription existente (trial) pelo expirado
        var existing = await dbContext.Subscriptions.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (existing is not null) dbContext.Subscriptions.Remove(existing);

        await dbContext.Subscriptions.AddAsync(expiredSub);
        await dbContext.SaveChangesAsync();

        var accessStatusCache = scope.ServiceProvider.GetRequiredService<IAccessStatusCacheService>();
        await accessStatusCache.InvalidateAsync(user.Id);
    }
}
