// US-057: consultar a execucao em andamento de uma quest, com exercicios ordenados.
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

public class QuestExecutionEndpointTests : IAsyncLifetime
{
    private const string WorkoutJson = """
        {
          "title": "Daily Quest",
          "exercises": [
            { "id": "ex-1", "name": "Squat", "sets": 3, "repsMin": 8, "repsMax": 12, "restSeconds": 60, "targetRpe": "8",
              "attributeContribution": { "strengthXp": 10, "agilityXp": 0, "enduranceXp": 0, "vitalityXp": 0, "focusXp": 0, "wisdomXp": 1 } },
            { "id": "ex-2", "name": "Push-up", "sets": 3, "repsMin": 10, "repsMax": 15, "restSeconds": 45, "targetRpe": "7",
              "attributeContribution": { "strengthXp": 6, "agilityXp": 0, "enduranceXp": 0, "vitalityXp": 0, "focusXp": 0, "wisdomXp": 1 } }
          ]
        }
        """;

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

    private async Task<Guid> CreateQuestWithWorkoutAsync(string email, string type = "daily")
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        var quest = Quest.Create(user.Id, DateTime.UtcNow.Date, "pt-BR", $"exec-test-{Guid.NewGuid():N}");
        if (type != "daily")
        {
            typeof(Quest).GetProperty(nameof(Quest.Type))!.SetValue(quest, type);
        }
        quest.AssignWorkout(WorkoutJson, DateTime.UtcNow);
        dbContext.Quests.Add(quest);
        await dbContext.SaveChangesAsync();

        return quest.Id;
    }

    private async Task<Guid> CreatePendingQuestWithoutWorkoutAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        var quest = Quest.Create(user.Id, DateTime.UtcNow.Date, "pt-BR", $"exec-pending-{Guid.NewGuid():N}");
        dbContext.Quests.Add(quest);
        await dbContext.SaveChangesAsync();

        return quest.Id;
    }

    [Theory]
    [InlineData("daily")]
    [InlineData("dungeon")]
    [InlineData("raid")]
    public async Task CA001_ReturnsOrderedExercises_ForInProgressQuest(string type)
    {
        var email = $"questexec_{type}@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateQuestWithWorkoutAsync(email, type);

        var startResponse = await _client.PostAsync($"/api/quests/{questId}/start", null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _client.GetAsync($"/api/quests/{questId}/execution");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<QuestExecutionResponse>();
        result!.QuestId.Should().Be(questId);
        result.Status.Should().Be("in_progress");
        result.AttributeXpPreview.Strength.Should().BeGreaterThan(0);
        result.AttributeXpPreview.Wisdom.Should().Be(2);
        result.Exercises.Should().HaveCount(2);
        result.Exercises.Select(e => e.Order).Should().ContainInOrder(1, 2);
        result.Exercises.Select(e => e.Name).Should().ContainInOrder("Squat", "Push-up");
        result.Exercises[0].Status.Should().Be("pending");
        result.Exercises[0].XpReward.Should().BeGreaterThan(0);
        result.Exercises[0].EffectiveDifficulty.Should().BeGreaterThanOrEqualTo(1);
        result.Exercises[0].EffectiveDifficulty.Should().BeLessThanOrEqualTo(4);
        result.Exercises[0].AttributeImpacts.Strength.Should().BeGreaterThanOrEqualTo(1);
        result.Exercises[0].AttributeImpacts.Strength.Should().BeLessThanOrEqualTo(4);
        result.Exercises[0].HiddenAttributeImpacts.Wisdom.Should().Be(1);
    }

    [Fact]
    public async Task Returns409_WhenQuestNotStarted()
    {
        const string email = "questexec_notstarted@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateQuestWithWorkoutAsync(email);

        var response = await _client.GetAsync($"/api/quests/{questId}/execution");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("QUEST_NOT_IN_PROGRESS");
    }

    [Fact]
    public async Task Returns409_WhenQuestHasNoExercises()
    {
        const string email = "questexec_noexercises@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreatePendingQuestWithoutWorkoutAsync(email);

        var startResponse = await _client.PostAsync($"/api/quests/{questId}/start", null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _client.GetAsync($"/api/quests/{questId}/execution");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("QUEST_HAS_NO_EXERCISES");
    }

    [Fact]
    public async Task Returns404_WhenQuestBelongsToAnotherUser()
    {
        const string ownerEmail = "questexec_owner_a@awaken.app";
        await AuthenticateNewHunterAsync(ownerEmail);
        var questId = await CreateQuestWithWorkoutAsync(ownerEmail);
        await _client.PostAsync($"/api/quests/{questId}/start", null);

        await AuthenticateNewHunterAsync("questexec_owner_b@awaken.app");
        var response = await _client.GetAsync($"/api/quests/{questId}/execution");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns404_WhenQuestDoesNotExist()
    {
        await AuthenticateNewHunterAsync("questexec_missing@awaken.app");

        var response = await _client.GetAsync($"/api/quests/{Guid.NewGuid()}/execution");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
