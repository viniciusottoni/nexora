// US-063: retornar o resumo de recompensa de uma quest concluida, baseado no QuestLog.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Application.Quests.Common;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Quests;
using Awaken.Domain.Entities.Progression;
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

public class QuestRewardSummaryEndpointTests : IAsyncLifetime
{
    private const string WorkoutJson = """
        {
          "title": "Daily Quest",
          "exercises": [
            { "id": "ex-1", "name": "Squat", "sets": 3, "repsMin": 8, "repsMax": 12, "restSeconds": 60, "targetRpe": "8",
              "attributeContribution": { "strengthXp": 10, "agilityXp": 0, "enduranceXp": 0, "vitalityXp": 0, "focusXp": 0, "wisdomXp": 1 } }
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

    private async Task<Guid> CreateStartedQuestAsync(string email, string type = "daily")
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        var quest = Quest.Create(user.Id, DateTime.UtcNow.Date, "pt-BR", $"reward-summary-test-{Guid.NewGuid():N}");
        if (type != "daily")
            typeof(Quest).GetProperty(nameof(Quest.Type))!.SetValue(quest, type);
        quest.AssignWorkout(WorkoutJson, DateTime.UtcNow);
        quest.Start(DateTime.UtcNow, QuestExerciseSeedMapper.ParseSeeds(WorkoutJson));
        dbContext.Quests.Add(quest);
        await dbContext.SaveChangesAsync();

        return quest.Id;
    }

    private async Task CompleteAllExercisesAsync(Guid questId)
    {
        var execResponse = await _client.GetAsync($"/api/quests/{questId}/execution");
        var execution = await execResponse.Content.ReadFromJsonAsync<QuestExecutionResponse>();
        foreach (var exercise in execution!.Exercises)
        {
            await _client.PostAsJsonAsync(
                $"/api/quests/{questId}/exercises/{exercise.QuestExerciseId}/complete",
                new { setsCompleted = exercise.Sets, strongPainReported = false });
        }
    }

    private async Task SeedStrengthXpBufferAsync(string email, int strengthXp)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        var progression = await dbContext.HunterProgressions
            .SingleAsync(p => p.UserId == user.Id);
        progression.AddAttributeXp(strength: strengthXp, agility: 0, endurance: 0, vitality: 0, focus: 0, wisdom: 0,
            externalMultiplier: 1.0m, utcNow: DateTime.UtcNow);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task CA001_DailyWithoutItems_ReturnsRewardSummaryWithEmptyItems()
    {
        const string email = "rewardsummary_daily@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateStartedQuestAsync(email);
        await CompleteAllExercisesAsync(questId);
        await _client.PostAsync($"/api/quests/{questId}/complete", null);

        var response = await _client.GetAsync($"/api/quests/{questId}/reward-summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<QuestRewardSummaryResponse>();
        result!.QuestType.Should().Be("daily");
        result.XpEarned.Should().BeGreaterThan(0);
        result.StreakDays.Should().Be(1);
        result.ItemsEarned.Should().BeEmpty();
        result.AttributeXpEarned.Wisdom.Should().Be(1);
        result.AttributeLevelUps.Should().BeEmpty();
    }

    [Fact]
    public async Task CA001b_DailySummaryIncludesAttributeLevelUpsWhenPresent()
    {
        const string email = "rewardsummary_levelup@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateStartedQuestAsync(email);
        await SeedStrengthXpBufferAsync(email, 6);
        await CompleteAllExercisesAsync(questId);
        await _client.PostAsync($"/api/quests/{questId}/complete", null);

        var response = await _client.GetAsync($"/api/quests/{questId}/reward-summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<QuestRewardSummaryResponse>();
        result!.AttributeLevelUps.Should().Contain("strength");
        result.AttributeLevelUpDetails.Should().ContainSingle(levelUp =>
            levelUp.Attribute == "strength" &&
            levelUp.NewLevel == 6 &&
            levelUp.Source == "daily");
        result.AttributeXpEarned.Strength.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CA002_DungeonWithItems_ReturnsItemsInSummary()
    {
        const string email = "rewardsummary_dungeon@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateStartedQuestAsync(email, type: "dungeon");
        await CompleteAllExercisesAsync(questId);
        await _client.PostAsync($"/api/quests/{questId}/complete", null);

        var response = await _client.GetAsync($"/api/quests/{questId}/reward-summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<QuestRewardSummaryResponse>();
        result!.QuestType.Should().Be("dungeon");
        result.ItemsEarned.Should().NotBeEmpty();
        result.ItemRewards.Should().ContainSingle(reward =>
            reward.ItemId == "pedra_dungeon" &&
            reward.Rarity == "consumable" &&
            reward.Source == "dungeon");
    }

    [Fact]
    public async Task ThrowsConflict_WhenQuestNotCompletedYet()
    {
        const string email = "rewardsummary_notcompleted@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateStartedQuestAsync(email);

        var response = await _client.GetAsync($"/api/quests/{questId}/reward-summary");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("QUEST_NOT_COMPLETED");
    }

    [Fact]
    public async Task Returns404_WhenQuestBelongsToAnotherUser()
    {
        await AuthenticateNewHunterAsync("rewardsummary_owner_a@awaken.app");
        var questId = await CreateStartedQuestAsync("rewardsummary_owner_a@awaken.app");
        await CompleteAllExercisesAsync(questId);
        await _client.PostAsync($"/api/quests/{questId}/complete", null);

        await AuthenticateNewHunterAsync("rewardsummary_owner_b@awaken.app");
        var response = await _client.GetAsync($"/api/quests/{questId}/reward-summary");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
