// US-059: pausar e retomar uma quest em andamento e idempotente (RN-001/RN-002/RN-003/RN-004),
// e bloqueado por acesso expirado (ActiveAccessMiddleware, 403) antes de chegar aqui.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Quests;
using Awaken.Domain.Entities.Quests;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class PauseResumeQuestEndpointTests : IAsyncLifetime
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

    private async Task<Guid> CreateInProgressQuestAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        var quest = Quest.Create(user.Id, DateTime.UtcNow.Date, "pt-BR", $"pause-test-{Guid.NewGuid():N}");
        quest.Start(DateTime.UtcNow, Array.Empty<QuestExerciseSeed>());
        dbContext.Quests.Add(quest);
        await dbContext.SaveChangesAsync();

        return quest.Id;
    }

    [Fact]
    public async Task CA001_PausingInProgressQuest_TransitionsToPaused()
    {
        const string email = "quest_pause_active@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateInProgressQuestAsync(email);

        var response = await _client.PostAsync($"/api/quests/{questId}/pause", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PauseQuestResponse>();
        result!.Status.Should().Be("paused");
        result.QuestId.Should().Be(questId);
    }

    [Fact]
    public async Task CA002_ResumingPausedQuest_TransitionsToInProgress()
    {
        const string email = "quest_resume_active@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateInProgressQuestAsync(email);
        await _client.PostAsync($"/api/quests/{questId}/pause", null);

        var response = await _client.PostAsync($"/api/quests/{questId}/resume", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ResumeQuestResponse>();
        result!.Status.Should().Be("in_progress");
        result.QuestId.Should().Be(questId);
    }

    [Fact]
    public async Task RN_PausingAlreadyPausedQuest_IsIdempotent()
    {
        const string email = "quest_pause_idempotent@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateInProgressQuestAsync(email);

        var first = await _client.PostAsync($"/api/quests/{questId}/pause", null);
        var firstResult = await first.Content.ReadFromJsonAsync<PauseQuestResponse>();

        var second = await _client.PostAsync($"/api/quests/{questId}/pause", null);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondResult = await second.Content.ReadFromJsonAsync<PauseQuestResponse>();

        secondResult!.PausedAtUtc.Should().Be(firstResult!.PausedAtUtc);
    }

    [Fact]
    public async Task RN006_PausingCompletedQuest_Returns409()
    {
        const string email = "quest_pause_completed@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateInProgressQuestAsync(email);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var quest = await dbContext.Quests.SingleAsync(q => q.Id == questId);
            quest.Complete(0, DateTime.UtcNow);
            await dbContext.SaveChangesAsync();
        }

        var response = await _client.PostAsync($"/api/quests/{questId}/pause", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("QUEST_NOT_PAUSABLE");
    }

    [Fact]
    public async Task RN002_ResumingPendingQuest_Returns409()
    {
        const string email = "quest_resume_pending@awaken.app";
        await AuthenticateNewHunterAsync(email);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        var quest = Quest.Create(user.Id, DateTime.UtcNow.Date, "pt-BR", $"resume-pending-{Guid.NewGuid():N}");
        dbContext.Quests.Add(quest);
        await dbContext.SaveChangesAsync();

        var response = await _client.PostAsync($"/api/quests/{quest.Id}/resume", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("QUEST_NOT_RESUMABLE");
    }

    [Fact]
    public async Task PausingQuestOfAnotherUser_Returns404()
    {
        await AuthenticateNewHunterAsync("quest_pause_owner_a@awaken.app");
        var questId = await CreateInProgressQuestAsync("quest_pause_owner_a@awaken.app");

        await AuthenticateNewHunterAsync("quest_pause_owner_b@awaken.app");
        var response = await _client.PostAsync($"/api/quests/{questId}/pause", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PausingNonExistentQuest_Returns404()
    {
        await AuthenticateNewHunterAsync("quest_pause_notfound@awaken.app");

        var response = await _client.PostAsync($"/api/quests/{Guid.NewGuid()}/pause", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
