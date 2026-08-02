// US-060: cancelar uma quest em andamento ou pausada (RN-001), impedindo conclusao
// posterior e recompensa completa (RN-002/RN-003).
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

public class CancelQuestEndpointTests : IAsyncLifetime
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

        var quest = Quest.Create(user.Id, DateTime.UtcNow.Date, "pt-BR", $"cancel-test-{Guid.NewGuid():N}");
        quest.Start(DateTime.UtcNow, Array.Empty<QuestExerciseSeed>());
        dbContext.Quests.Add(quest);
        await dbContext.SaveChangesAsync();

        return quest.Id;
    }

    [Fact]
    public async Task CA001_CancellingInProgressQuest_TransitionsToCancelled()
    {
        const string email = "quest_cancel_active@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateInProgressQuestAsync(email);

        var response = await _client.PostAsync($"/api/quests/{questId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CancelQuestResponse>();
        result!.Status.Should().Be("cancelled");
        result.QuestId.Should().Be(questId);
    }

    [Fact]
    public async Task CA001_CancellingPausedQuest_TransitionsToCancelled()
    {
        const string email = "quest_cancel_paused@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateInProgressQuestAsync(email);
        await _client.PostAsync($"/api/quests/{questId}/pause", null);

        var response = await _client.PostAsync($"/api/quests/{questId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CancelQuestResponse>();
        result!.Status.Should().Be("cancelled");
    }

    [Fact]
    public async Task RN_CancellingAlreadyCancelledQuest_IsIdempotent()
    {
        const string email = "quest_cancel_idempotent@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateInProgressQuestAsync(email);

        var first = await _client.PostAsync($"/api/quests/{questId}/cancel", null);
        var firstResult = await first.Content.ReadFromJsonAsync<CancelQuestResponse>();

        var second = await _client.PostAsync($"/api/quests/{questId}/cancel", null);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondResult = await second.Content.ReadFromJsonAsync<CancelQuestResponse>();

        secondResult!.CancelledAtUtc.Should().Be(firstResult!.CancelledAtUtc);
    }

    [Fact]
    public async Task RN002_CancellingCompletedQuest_Returns409()
    {
        const string email = "quest_cancel_completed@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateInProgressQuestAsync(email);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var quest = await dbContext.Quests.SingleAsync(q => q.Id == questId);
            quest.Complete(0, DateTime.UtcNow);
            await dbContext.SaveChangesAsync();
        }

        var response = await _client.PostAsync($"/api/quests/{questId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("QUEST_NOT_CANCELLABLE");
    }

    [Fact]
    public async Task RN002_StartingCancelledQuest_Returns409()
    {
        const string email = "quest_cancel_then_start@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateInProgressQuestAsync(email);
        await _client.PostAsync($"/api/quests/{questId}/cancel", null);

        var response = await _client.PostAsync($"/api/quests/{questId}/start", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("QUEST_NOT_STARTABLE");
    }

    [Fact]
    public async Task CancellingQuestOfAnotherUser_Returns404()
    {
        await AuthenticateNewHunterAsync("quest_cancel_owner_a@awaken.app");
        var questId = await CreateInProgressQuestAsync("quest_cancel_owner_a@awaken.app");

        await AuthenticateNewHunterAsync("quest_cancel_owner_b@awaken.app");
        var response = await _client.PostAsync($"/api/quests/{questId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancellingNonExistentQuest_Returns404()
    {
        await AuthenticateNewHunterAsync("quest_cancel_notfound@awaken.app");

        var response = await _client.PostAsync($"/api/quests/{Guid.NewGuid()}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
