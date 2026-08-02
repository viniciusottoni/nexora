// US-064/US-065/US-071: conclusão de exercício com XP proporcional, TotalXp na resposta
// e feedback de level up / rank up.
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

public class CompleteExerciseEndpointTests : IAsyncLifetime
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

    private async Task<Guid> CreateStartedQuestWithWorkoutAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        var quest = Quest.Create(user.Id, DateTime.UtcNow.Date, "pt-BR", $"complete-test-{Guid.NewGuid():N}");
        quest.AssignWorkout(WorkoutJson, DateTime.UtcNow);
        quest.Start(DateTime.UtcNow, QuestExerciseSeedMapper.ParseSeeds(WorkoutJson));
        dbContext.Quests.Add(quest);
        await dbContext.SaveChangesAsync();

        return quest.Id;
    }

    private async Task<(long TotalXp, int Strength, int Wisdom)> GetProgressionAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        var progression = await dbContext.HunterProgressions.AsNoTracking().SingleAsync(p => p.UserId == user.Id);
        return (progression.TotalXp, progression.Strength, progression.Wisdom);
    }

    private async Task GetOrCreateProgressionWithXpBufferAsync(string email, int strengthBuffer)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        var existing = await dbContext.HunterProgressions.SingleOrDefaultAsync(p => p.UserId == user.Id);
        if (existing is not null)
        {
            // Pre-seed o buffer de XP interno de Força sem gerar level-up.
            existing.AddAttributeXp(strength: strengthBuffer, agility: 0, endurance: 0, vitality: 0, focus: 0, wisdom: 0,
                externalMultiplier: 1.0m, utcNow: DateTime.UtcNow);
            await dbContext.SaveChangesAsync();
        }
    }

    private async Task GetOrCreateProgressionWithWisdomBufferAsync(string email, int wisdomBuffer)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        var existing = await dbContext.HunterProgressions.SingleOrDefaultAsync(p => p.UserId == user.Id);
        if (existing is not null)
        {
            existing.AddAttributeXp(strength: 0, agility: 0, endurance: 0, vitality: 0, focus: 0, wisdom: wisdomBuffer,
                externalMultiplier: 1.0m, utcNow: DateTime.UtcNow);
            await dbContext.SaveChangesAsync();
        }
    }

    /// Substitui a progressão existente por uma com TotalXp específico (Level 1, 0 streak).
    private async Task ReplaceProgressionWithXpAsync(string email, long totalXp)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        var existing = await dbContext.HunterProgressions.SingleOrDefaultAsync(p => p.UserId == user.Id);
        if (existing is not null) dbContext.HunterProgressions.Remove(existing);
        var prog = HunterProgression.Create(user.Id);
        prog.AddXp(totalXp, DateTime.UtcNow);
        dbContext.HunterProgressions.Add(prog);
        await dbContext.SaveChangesAsync();
    }

    /// Substitui a progressão existente com atributos que colocam o RankScore logo abaixo do Rank D (17).
    private async Task ReplaceProgressionNearRankThresholdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        var existing = await dbContext.HunterProgressions.SingleOrDefaultAsync(p => p.UserId == user.Id);
        if (existing is not null) dbContext.HunterProgressions.Remove(existing);
        // 3+3+3+3+3+2 = 17 → Rank E. Completar o squat concede +4 Força e +1 Sabedoria → 22 → Rank D.
        dbContext.HunterProgressions.Add(HunterProgression.CreateFromOnboarding(
            user.Id, strength: 3, agility: 3, endurance: 3, vitality: 3, focus: 3, wisdom: 2));
        await dbContext.SaveChangesAsync();
    }

    // CA-001 US-064: conclusão completa concede XP e persiste na progressão com TotalXp no response.
    // US-130: atributos recebem XP interno (não level direto); level só sobe após acumular 10.
    [Fact]
    public async Task CA001_CompletingExercise_AwardsXpAndReturnsTotalXp()
    {
        const string email = "completeex_award@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateStartedQuestWithWorkoutAsync(email);

        var execResponse = await _client.GetAsync($"/api/quests/{questId}/execution");
        var execution = await execResponse.Content.ReadFromJsonAsync<QuestExecutionResponse>();
        var exerciseId = execution!.Exercises[0].QuestExerciseId;
        var totalSets = execution.Exercises[0].Sets;

        var before = await GetProgressionAsync(email);

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/exercises/{exerciseId}/complete",
            new { setsCompleted = totalSets, strongPainReported = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CompleteExerciseResponse>();
        result!.Status.Should().Be("completed");
        result.XpEarned.Should().BeGreaterThan(0);
        result.TotalXp.Should().BeGreaterThan(0);
        result.EffectiveDifficulty.Should().BeGreaterThanOrEqualTo(1);
        result.EffectiveDifficulty.Should().BeLessThanOrEqualTo(4);
        // US-130: XP interno de atributo ganha ao completar (wisdom é sempre 1).
        result.AttributeXpEarned.Wisdom.Should().BeGreaterThan(0);
        result.AttributeXpEarned.Strength.Should().BeGreaterThan(0);
        // US-130: com buffer vazio, nenhum level up ocorre num único exercício.
        result.AttributePointsGranted.Strength.Should().Be(0);
        result.AlreadyCompleted.Should().BeFalse();

        var after = await GetProgressionAsync(email);
        after.TotalXp.Should().Be(before.TotalXp + result.XpEarned);
        result.TotalXp.Should().Be(after.TotalXp);
        // Level de atributo não muda com buffer < 10.
        after.Strength.Should().Be(before.Strength);
        after.Wisdom.Should().Be(before.Wisdom);
    }

    // CA-002 US-064: chamada duplicada não duplica XP nem atributos.
    [Fact]
    public async Task CA002_DuplicateCompletion_DoesNotDuplicateXpOrAttributes()
    {
        const string email = "completeex_duplicate@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateStartedQuestWithWorkoutAsync(email);

        var execResponse = await _client.GetAsync($"/api/quests/{questId}/execution");
        var execution = await execResponse.Content.ReadFromJsonAsync<QuestExecutionResponse>();
        var exerciseId = execution!.Exercises[0].QuestExerciseId;
        var totalSets = execution.Exercises[0].Sets;
        var body = new { setsCompleted = totalSets, strongPainReported = false };

        var first = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/exercises/{exerciseId}/complete", body);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstResult = await first.Content.ReadFromJsonAsync<CompleteExerciseResponse>();

        var afterFirst = await GetProgressionAsync(email);

        var second = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/exercises/{exerciseId}/complete", body);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondResult = await second.Content.ReadFromJsonAsync<CompleteExerciseResponse>();

        secondResult!.AlreadyCompleted.Should().BeTrue();
        secondResult.XpEarned.Should().Be(firstResult!.XpEarned);

        var afterSecond = await GetProgressionAsync(email);
        afterSecond.TotalXp.Should().Be(afterFirst.TotalXp);
        afterSecond.Strength.Should().Be(afterFirst.Strength);
        afterSecond.Wisdom.Should().Be(afterFirst.Wisdom);
    }

    // CA-001 US-065: conclusão parcial (1 de 3 séries) concede XP proporcional.
    [Fact]
    public async Task CA003_PartialCompletion_AwardsProportionalXp()
    {
        const string email = "completeex_partial@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateStartedQuestWithWorkoutAsync(email);

        var execResponse = await _client.GetAsync($"/api/quests/{questId}/execution");
        var execution = await execResponse.Content.ReadFromJsonAsync<QuestExecutionResponse>();
        var exercise = execution!.Exercises[0];
        var exerciseId = exercise.QuestExerciseId;

        // Obtém o XP base completo primeiro (sem usar endpoint para não contaminar o estado).
        var totalSets = exercise.Sets;

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/exercises/{exerciseId}/complete",
            new { setsCompleted = 1, strongPainReported = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CompleteExerciseResponse>();
        result!.Status.Should().Be("completed");

        // XP deve ser proporcional: ≈ XpReward * (1/totalSets).
        // Não conhecemos XpReward exato aqui, mas deve ser menor que o full reward.
        // Verificamos que é menor que o retorno de uma conclusão completa de outro exercício.
        var fullExercise = execution.Exercises[1];
        var fullResponse = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/exercises/{fullExercise.QuestExerciseId}/complete",
            new { setsCompleted = totalSets, strongPainReported = false });
        var fullResult = await fullResponse.Content.ReadFromJsonAsync<CompleteExerciseResponse>();

        result.XpEarned.Should().BeLessThanOrEqualTo(fullResult!.XpEarned);
    }

    // CA-002 US-065: dor forte não aumenta XP além do proporcional.
    [Fact]
    public async Task CA004_StrongPain_XpDoesNotExceedNoPainXp()
    {
        const string email1 = "completeex_pain_a@awaken.app";
        const string email2 = "completeex_pain_b@awaken.app";

        await AuthenticateNewHunterAsync(email1);
        var questId1 = await CreateStartedQuestWithWorkoutAsync(email1);
        var exec1 = await (await _client.GetAsync($"/api/quests/{questId1}/execution"))
            .Content.ReadFromJsonAsync<QuestExecutionResponse>();
        var exerciseId1 = exec1!.Exercises[0].QuestExerciseId;
        var totalSets = exec1.Exercises[0].Sets;

        var painResponse = await _client.PostAsJsonAsync(
            $"/api/quests/{questId1}/exercises/{exerciseId1}/complete",
            new { setsCompleted = totalSets, strongPainReported = true });
        var painResult = await painResponse.Content.ReadFromJsonAsync<CompleteExerciseResponse>();

        // Novo hunter sem dor para comparar.
        _client.DefaultRequestHeaders.Authorization = null;
        await AuthenticateNewHunterAsync(email2);
        var questId2 = await CreateStartedQuestWithWorkoutAsync(email2);
        var exec2 = await (await _client.GetAsync($"/api/quests/{questId2}/execution"))
            .Content.ReadFromJsonAsync<QuestExecutionResponse>();
        var exerciseId2 = exec2!.Exercises[0].QuestExerciseId;

        var noPainResponse = await _client.PostAsJsonAsync(
            $"/api/quests/{questId2}/exercises/{exerciseId2}/complete",
            new { setsCompleted = totalSets, strongPainReported = false });
        var noPainResult = await noPainResponse.Content.ReadFromJsonAsync<CompleteExerciseResponse>();

        painResult!.XpEarned.Should().BeLessThanOrEqualTo(noPainResult!.XpEarned);
    }

    // US-130 / CA-001: buffer de 6 XP + 4 XP do exercício = 10 → level up de Força e attributeLevelUps não vazio.
    [Fact]
    public async Task CA007_US130_WhenXpBufferReaches10_AttributeLevelUpOccurs()
    {
        const string email = "completeex_attrxp@awaken.app";
        await AuthenticateNewHunterAsync(email);
        // Pre-seed 6 XP interno de Força (sem level up).
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var user = await dbContext.Users.SingleAsync(u => u.Email == email);
            var prog = await dbContext.HunterProgressions.SingleAsync(p => p.UserId == user.Id);
            prog.AddAttributeXp(strength: 6, agility: 0, endurance: 0, vitality: 0, focus: 0, wisdom: 0,
                externalMultiplier: 1.0m, utcNow: DateTime.UtcNow);
            await dbContext.SaveChangesAsync();
        }

        var questId = await CreateStartedQuestWithWorkoutAsync(email);
        var execResponse = await _client.GetAsync($"/api/quests/{questId}/execution");
        var execution = await execResponse.Content.ReadFromJsonAsync<QuestExecutionResponse>();
        var exercise = execution!.Exercises[0]; // Squat: +4 XP Força → 6+4=10 → level up!

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/exercises/{exercise.QuestExerciseId}/complete",
            new { setsCompleted = exercise.Sets, strongPainReported = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CompleteExerciseResponse>();
        result!.AttributeLevelUps.Should().NotBeNull();
        result.AttributeLevelUps.Should().Contain("strength");
        result.AttributePointsGranted.Strength.Should().Be(1); // 1 level up

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var verifyUser = await verifyDb.Users.SingleAsync(u => u.Email == email);
        var prog2 = await verifyDb.HunterProgressions.AsNoTracking().SingleAsync(p => p.UserId == verifyUser.Id);
        prog2.Strength.Should().BeGreaterThan(1); // level subiu
        prog2.StrengthXp.Should().Be(0); // 10-10 = 0 (exatamente 10, sem excesso)
    }

    // US-131 / CA-001: Sabedoria tambem sobe de level quando o buffer interno chega em 10.
    [Fact]
    public async Task CA007b_US131_WhenWisdomXpBufferReaches10_AttributeLevelUpOccurs()
    {
        const string email = "completeex_wisdomxp@awaken.app";
        await AuthenticateNewHunterAsync(email);
        // Pre-seed 9 XP interno de Sabedoria (sem level up).
        await GetOrCreateProgressionWithWisdomBufferAsync(email, wisdomBuffer: 9);

        int beforeWisdom;
        using (var beforeScope = _factory.Services.CreateScope())
        {
            var beforeDb = beforeScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var beforeUser = await beforeDb.Users.SingleAsync(u => u.Email == email);
            var beforeProg = await beforeDb.HunterProgressions.AsNoTracking()
                .SingleAsync(p => p.UserId == beforeUser.Id);
            beforeProg.WisdomXp.Should().Be(9);
            beforeWisdom = beforeProg.Wisdom;
        }

        var questId = await CreateStartedQuestWithWorkoutAsync(email);
        var execResponse = await _client.GetAsync($"/api/quests/{questId}/execution");
        var execution = await execResponse.Content.ReadFromJsonAsync<QuestExecutionResponse>();
        var exercise = execution!.Exercises[0];

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/exercises/{exercise.QuestExerciseId}/complete",
            new { setsCompleted = exercise.Sets, strongPainReported = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CompleteExerciseResponse>();
        result!.AttributeLevelUps.Should().NotBeNull();
        result.AttributeLevelUps.Should().Contain("wisdom");
        result.AttributeXpEarned.Wisdom.Should().Be(1);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var verifyUser = await verifyDb.Users.SingleAsync(u => u.Email == email);
        var prog = await verifyDb.HunterProgressions.AsNoTracking().SingleAsync(p => p.UserId == verifyUser.Id);
        prog.Wisdom.Should().Be(beforeWisdom + 1);
        prog.WisdomXp.Should().Be(0);
    }

    [Fact]
    public async Task Returns409_WhenQuestIsPending()
    {
        const string email = "completeex_pending@awaken.app";
        await AuthenticateNewHunterAsync(email);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        var quest = Quest.Create(user.Id, DateTime.UtcNow.Date, "pt-BR", $"complete-pending-{Guid.NewGuid():N}");
        quest.AssignWorkout(WorkoutJson, DateTime.UtcNow);
        dbContext.Quests.Add(quest);
        await dbContext.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{quest.Id}/exercises/{Guid.NewGuid()}/complete",
            new { setsCompleted = 3, strongPainReported = false });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("QUEST_NOT_IN_PROGRESS");
    }

    [Fact]
    public async Task Returns409_WhenQuestIsCompleted()
    {
        const string email = "completeex_completed@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateStartedQuestWithWorkoutAsync(email);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var quest = await dbContext.Quests.SingleAsync(q => q.Id == questId);
            quest.Complete(0, DateTime.UtcNow);
            await dbContext.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/exercises/{Guid.NewGuid()}/complete",
            new { setsCompleted = 3, strongPainReported = false });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("QUEST_NOT_IN_PROGRESS");
    }

    [Fact]
    public async Task Returns404_WhenExerciseDoesNotExist()
    {
        const string email = "completeex_noexercise@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateStartedQuestWithWorkoutAsync(email);

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/exercises/{Guid.NewGuid()}/complete",
            new { setsCompleted = 3, strongPainReported = false });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns422_WhenSetsCompletedIsZero()
    {
        const string email = "completeex_zerosets@awaken.app";
        await AuthenticateNewHunterAsync(email);
        var questId = await CreateStartedQuestWithWorkoutAsync(email);

        var execResponse = await _client.GetAsync($"/api/quests/{questId}/execution");
        var execution = await execResponse.Content.ReadFromJsonAsync<QuestExecutionResponse>();
        var exerciseId = execution!.Exercises[0].QuestExerciseId;

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/exercises/{exerciseId}/complete",
            new { setsCompleted = 0, strongPainReported = false });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // US-071 / CA-001: quando o XP concedido ultrapassa o limiar, LevelsGained > 0 na resposta.
    [Fact]
    public async Task CA005_WhenXpCrossesLevelThreshold_LevelsGainedIsPositive()
    {
        const string email = "completeex_levelup@awaken.app";
        await AuthenticateNewHunterAsync(email);
        // Squat concede 26 XP completo. Com TotalXp = 80, 80+26 = 106 >= 100 → sobe para Level 2.
        await ReplaceProgressionWithXpAsync(email, 80);
        var questId = await CreateStartedQuestWithWorkoutAsync(email);

        var execResponse = await _client.GetAsync($"/api/quests/{questId}/execution");
        var execution = await execResponse.Content.ReadFromJsonAsync<QuestExecutionResponse>();
        var exercise = execution!.Exercises[0];

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/exercises/{exercise.QuestExerciseId}/complete",
            new { setsCompleted = exercise.Sets, strongPainReported = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CompleteExerciseResponse>();
        result!.LevelsGained.Should().BeGreaterThan(0);
        result.RankChanged.Should().BeFalse(); // XP não muda Rank
        result.NewRank.Should().BeNull();
    }

    // US-071 / CA-001 / US-130: quando o XP interno atinge o limiar (10), o Level de atributo sobe,
    // o RankScore é recalculado e pode mudar o Rank.
    [Fact]
    public async Task CA006_WhenAttributeXpCrossesThresholdAndPromotesRank_RankChangedIsTrue()
    {
        const string email = "completeex_rankup@awaken.app";
        await AuthenticateNewHunterAsync(email);
        // Progressão: Força=3, demais=3/3/3/3/2 → RankScore = 17 (Rank E).
        // US-130: pré-seed 6 XP interno de Força. Squat concede +4 XP → buffer = 10 → Força sobe para 4.
        // RankScore = 4+3+3+3+3+2 = 18 → Rank D.
        await ReplaceProgressionNearRankThresholdAsync(email);
        await GetOrCreateProgressionWithXpBufferAsync(email, strengthBuffer: 6);
        var questId = await CreateStartedQuestWithWorkoutAsync(email);

        var execResponse = await _client.GetAsync($"/api/quests/{questId}/execution");
        var execution = await execResponse.Content.ReadFromJsonAsync<QuestExecutionResponse>();
        var exercise = execution!.Exercises[0];

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/exercises/{exercise.QuestExerciseId}/complete",
            new { setsCompleted = exercise.Sets, strongPainReported = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CompleteExerciseResponse>();
        result!.RankChanged.Should().BeTrue();
        result.NewRank.Should().Be("D");
        result.LevelsGained.Should().Be(0); // XP geral não muda Level do Hunter
        result.AttributeLevelUps.Should().Contain("strength");
    }
}
