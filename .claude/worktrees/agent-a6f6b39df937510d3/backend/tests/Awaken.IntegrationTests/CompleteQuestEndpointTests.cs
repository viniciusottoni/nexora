// US-061/US-062: concluir a execucao da quest, consolidando o resultado final ja
// recompensado por exercicio, atualizando o streak e registrando o QuestLog (idempotente).
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Application.Quests.Common;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Quests;
using Awaken.Domain.Entities.Inventory;
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

public class CompleteQuestEndpointTests : IAsyncLifetime
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

        var quest = Quest.Create(user.Id, DateTime.UtcNow.Date, "pt-BR", $"complete-quest-test-{Guid.NewGuid():N}");
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
            // CompleteExerciseRequest.SetsCompleted é obrigatório (US-064/US-065) - sem corpo,
            // o model binding falha (400) e o exercício nunca é marcado como concluído, o que
            // zera o XpEarned somado em CompleteQuestCommandHandler (bug do helper de teste,
            // não do endpoint: o cliente real sempre envia setsCompleted).
            var response = await _client.PostAsJsonAsync(
                $"/api/quests/{questId}/exercises/{exercise.QuestExerciseId}/complete",
                new { setsCompleted = exercise.Sets });
            response.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task CA001_CompletingDaily_ConsolidatesXpAndMarksCompleted()
    {
        const string email = "completequest_daily@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateStartedQuestAsync(email);
        await CompleteAllExercisesAsync(questId);

        var response = await _client.PostAsync($"/api/quests/{questId}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CompleteQuestResponse>();
        result!.Status.Should().Be("completed");
        result.QuestType.Should().Be("daily");
        result.XpEarned.Should().BeGreaterThan(0);
        result.ItemsEarned.Should().BeEmpty();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var log = await dbContext.QuestLogs.SingleAsync(l => l.QuestId == questId);
        log.QuestType.Should().Be("daily");
        log.XpEarned.Should().Be(result.XpEarned);
    }

    // US-241 §6.2: "como você se sentiu?" enviado no corpo do complete é persistido no QuestLog.
    [Fact]
    public async Task US241_PersistsPerceivedFeelingFromRequestBody()
    {
        const string email = "completequest_feeling@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateStartedQuestAsync(email);
        await CompleteAllExercisesAsync(questId);

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/complete", new CompleteQuestRequest("too_easy"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var log = await dbContext.QuestLogs.SingleAsync(l => l.QuestId == questId);
        log.PerceivedFeeling.Should().Be("too_easy");
    }

    [Fact]
    public async Task RN007_CompletingDungeon_GrantsItemToInventory()
    {
        const string email = "completequest_dungeon@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateStartedQuestAsync(email, type: "dungeon");
        await CompleteAllExercisesAsync(questId);

        var response = await _client.PostAsync($"/api/quests/{questId}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CompleteQuestResponse>();
        result!.QuestType.Should().Be("dungeon");
        result.ItemsEarned.Should().ContainSingle().Which.Should().Be(ItemKeys.DungeonStone);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        var inventoryItem = await dbContext.InventoryItems
            .SingleAsync(i => i.UserId == user.Id && i.ItemKey == ItemKeys.DungeonStone);
        inventoryItem.Quantity.Should().Be(1);
    }

    [Fact]
    public async Task RN009_CompletingRaid_UsesRaidQuestType()
    {
        const string email = "completequest_raid@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateStartedQuestAsync(email, type: "raid");
        await CompleteAllExercisesAsync(questId);

        var response = await _client.PostAsync($"/api/quests/{questId}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CompleteQuestResponse>();
        result!.QuestType.Should().Be("raid");
    }

    [Fact]
    public async Task CA002_DuplicateCompletion_DoesNotDuplicateRewardOrLog()
    {
        const string email = "completequest_duplicate@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateStartedQuestAsync(email, type: "dungeon");
        await CompleteAllExercisesAsync(questId);

        var first = await _client.PostAsync($"/api/quests/{questId}/complete", null);
        var firstResult = await first.Content.ReadFromJsonAsync<CompleteQuestResponse>();

        var second = await _client.PostAsync($"/api/quests/{questId}/complete", null);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondResult = await second.Content.ReadFromJsonAsync<CompleteQuestResponse>();

        secondResult!.XpEarned.Should().Be(firstResult!.XpEarned);
        secondResult.ItemsEarned.Should().BeEquivalentTo(firstResult.ItemsEarned);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        (await dbContext.QuestLogs.CountAsync(l => l.QuestId == questId)).Should().Be(1);
        var inventoryItem = await dbContext.InventoryItems
            .SingleAsync(i => i.UserId == user.Id && i.ItemKey == ItemKeys.DungeonStone);
        inventoryItem.Quantity.Should().Be(1);
    }

    [Fact]
    public async Task RN002_CompletingCancelledQuest_Returns409()
    {
        const string email = "completequest_cancelled@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateStartedQuestAsync(email);
        await _client.PostAsync($"/api/quests/{questId}/cancel", null);

        var response = await _client.PostAsync($"/api/quests/{questId}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("QUEST_NOT_COMPLETABLE");
    }

    [Fact]
    public async Task Returns409_WhenQuestIsPending()
    {
        const string email = "completequest_pending@awaken.app";
        await AuthenticateNewHunterAsync(email);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        var quest = Quest.Create(user.Id, DateTime.UtcNow.Date, "pt-BR", $"completequest-pending-{Guid.NewGuid():N}");
        dbContext.Quests.Add(quest);
        await dbContext.SaveChangesAsync();

        var response = await _client.PostAsync($"/api/quests/{quest.Id}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("QUEST_NOT_COMPLETABLE");
    }

    [Fact]
    public async Task CompletingQuestOfAnotherUser_Returns404()
    {
        await AuthenticateNewHunterAsync("completequest_owner_a@awaken.app");
        var questId = await CreateStartedQuestAsync("completequest_owner_a@awaken.app");

        await AuthenticateNewHunterAsync("completequest_owner_b@awaken.app");
        var response = await _client.PostAsync($"/api/quests/{questId}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompletingNonExistentQuest_Returns404()
    {
        await AuthenticateNewHunterAsync("completequest_notfound@awaken.app");

        var response = await _client.PostAsync($"/api/quests/{Guid.NewGuid()}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
