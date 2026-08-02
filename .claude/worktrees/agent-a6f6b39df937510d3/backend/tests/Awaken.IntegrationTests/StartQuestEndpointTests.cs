// US-056: iniciar a execucao de uma quest (daily/dungeon/raid) e idempotente
// quando ja em andamento (RN-003), bloqueado por acesso expirado (RN-006, CA-002)
// pelo ActiveAccessMiddleware (403), e retorna 404/409 para quest invalida.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Quests;
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

public class StartQuestEndpointTests : IAsyncLifetime
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

    private async Task AuthenticateNewHunterAsync(string email)
    {
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
    }

    private async Task<Guid> CreatePendingQuestAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        var quest = Quest.Create(user.Id, DateTime.UtcNow.Date, "pt-BR", $"start-test-{Guid.NewGuid():N}");
        dbContext.Quests.Add(quest);
        await dbContext.SaveChangesAsync();

        return quest.Id;
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
    public async Task CA001_StartingPendingQuest_WithActiveAccess_TransitionsToInProgress()
    {
        const string email = "quest_start_active@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreatePendingQuestAsync(email);

        var response = await _client.PostAsync($"/api/quests/{questId}/start", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<StartQuestResponse>();
        result!.Status.Should().Be("in_progress");
        result.QuestId.Should().Be(questId);
    }

    [Fact]
    public async Task CA002_StartingQuest_WhenAccessExpired_IsBlockedWith403()
    {
        const string email = "quest_start_blocked@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreatePendingQuestAsync(email);

        var expiresAt = DateTime.UtcNow.AddDays(-1);
        await SeedPaidSubscriptionAsync(email, "monthly", expiresAt, "rc_quest_start_blocked_test");

        var response = await _client.PostAsync($"/api/quests/{questId}/start", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("ACCESS_BLOCKED");
    }

    [Fact]
    public async Task RN003_StartingAlreadyInProgressQuest_IsIdempotent()
    {
        const string email = "quest_start_idempotent@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreatePendingQuestAsync(email);

        var first = await _client.PostAsync($"/api/quests/{questId}/start", null);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstResult = await first.Content.ReadFromJsonAsync<StartQuestResponse>();

        var second = await _client.PostAsync($"/api/quests/{questId}/start", null);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondResult = await second.Content.ReadFromJsonAsync<StartQuestResponse>();

        secondResult!.Status.Should().Be("in_progress");
        secondResult.StartedAtUtc.Should().Be(firstResult!.StartedAtUtc);
    }

    [Fact]
    public async Task StartingNonExistentQuest_Returns404()
    {
        await AuthenticateNewHunterAsync("quest_start_notfound@awaken.app");

        var response = await _client.PostAsync($"/api/quests/{Guid.NewGuid()}/start", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartingQuestOfAnotherUser_Returns404()
    {
        await AuthenticateNewHunterAsync("quest_start_owner_a@awaken.app");
        var questId = await CreatePendingQuestAsync("quest_start_owner_a@awaken.app");

        await AuthenticateNewHunterAsync("quest_start_owner_b@awaken.app");
        var response = await _client.PostAsync($"/api/quests/{questId}/start", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartingCompletedQuest_Returns409()
    {
        const string email = "quest_start_completed@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreatePendingQuestAsync(email);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var quest = await dbContext.Quests.SingleAsync(q => q.Id == questId);
            quest.Complete(0, DateTime.UtcNow);
            await dbContext.SaveChangesAsync();
        }

        var response = await _client.PostAsync($"/api/quests/{questId}/start", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("QUEST_NOT_STARTABLE");
    }
}
