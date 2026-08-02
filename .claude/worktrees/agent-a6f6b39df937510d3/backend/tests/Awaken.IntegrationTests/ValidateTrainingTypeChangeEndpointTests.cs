﻿// US-053: validacao (dry-run) da troca de tipo de treino.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Quests;
using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Entities.Training;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class ValidateTrainingTypeChangeEndpointTests : IAsyncLifetime
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

    private async Task SeedApprovedExerciseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        if (await db.ExerciseCatalogs.AnyAsync()) return;

        var snapshot = new ExerciseCatalogSnapshot(
            RawImportId: null, ProviderName: "test", ProviderExerciseId: "validate-type-ex001",
            ProviderVersion: null, NamePtBr: "Squat", NameOriginal: "Squat", Slug: "validate-type-ex001",
            DescriptionPtBr: "Agachamento", InstructionsPtBr: ["DesÃ§a devagar"],
            InstructionsOriginal: ["Go down slowly"], TipsPtBr: [], ExerciseType: "strength",
            MovementPattern: "squat", MovementFamily: "legs", Mechanic: "compound",
            ForceType: "push", PlaneOfMotion: "sagittal", Laterality: "bilateral",
            BodyPosition: "standing", BenchAngle: null, EquipmentCategory: "bodyweight",
            LoadType: "bodyweight", PrimaryRegion: "lower_body", DifficultyLevel: "intermediate",
            DifficultyRank: 2, TechnicalComplexity: 2, ImpactLevel: 2, Environment: "home",
            RequiredEquipment: [], PrimaryMuscleGroups: ["quadriceps"],
            SecondaryMuscleGroups: ["glutes"], BodyParts: ["legs"], JointStressTags: [],
            ContraindicationTags: [], LimitationBlockTags: [], PainBlockTags: [],
            GoalTags: ["gain_muscle", "strength"], RiskTags: [], AccessibilityTags: [],
            TaxonomySignals: [], MinExperienceLevel: "beginner", SuitableForSedentary: true,
            SuitableForBeginner: true, SuitableForIntermediate: true, SuitableForAdvanced: true,
            IsCompound: true, IsUnilateral: false, IsAssisted: false, IsWeighted: false,
            RegressionExerciseIds: [], ProgressionExerciseIds: [], RelatedExerciseIds: [],
            VideoUrl: "https://video.example/validate-type-ex001", ImageUrl: null, GifUrl: null,
            MediaLicenseInfo: null, SanitizationStatus: "approved",
            IsApprovedForWorkoutGeneration: true, Confidence: "high");

        var exercise = ExerciseCatalog.Create(snapshot, DateTime.UtcNow);
        exercise.SetAttributeContribution(ExerciseAttributeContribution.CreateAutoGenerated(
            primaryAttribute: "strength", strengthXp: 10, agilityXp: 0, enduranceXp: 0,
            vitalityXp: 0, focusXp: 0, wisdomXp: 1), DateTime.UtcNow);
        db.ExerciseCatalogs.Add(exercise);
        await db.SaveChangesAsync();
    }

    private async Task<Guid> GenerateQuestAsync()
    {
        var response = await _client.PostAsync("/api/quests/daily/generate", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<QuestResponse>())!.Id;
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

    // â"€â"€ CA-001: tipo valido retorna recalculo sem persistir â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

    [Fact]
    public async Task CA001_Returns200_WithValidTrue_ForRegeneration()
    {
        var token = await RegisterAndGetTokenAsync("vt-regen@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();
        await SeedApprovedExerciseAsync();
        var questId = await GenerateQuestAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/validate-training-type-change",
            new { trainingType = "regeneration" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ValidateTrainingTypeChangeResponse>();
        body!.Valid.Should().BeTrue();
        body.EstimatedDurationMinutes.Should().BeGreaterThan(0);
        body.EstimatedXp.Should().BeGreaterThan(0);

        // Dry-run: a quest no banco continua nao alterada.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var quest = await db.Quests.AsNoTracking().SingleAsync(q => q.Id == questId);
        quest.Status.Should().Be("pending");
        quest.TrainingType.Should().Be("program");
        quest.ProgramId.Should().Be(TrainingProgramKeys.FullBody);
    }

    [Fact]
    public async Task CA001_Returns200_WithValidTrue_ForSaitamaPath()
    {
        var token = await RegisterAndGetTokenAsync("vt-saitama@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();
        await SeedApprovedExerciseAsync();
        var questId = await GenerateQuestAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/validate-training-type-change",
            new { trainingType = "program", programId = "saitama_path" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ValidateTrainingTypeChangeResponse>();
        body!.Valid.Should().BeTrue();
        body.EstimatedDurationMinutes.Should().Be(60);
        body.EstimatedXp.Should().Be(240);
    }

    // â"€â"€ Tipo invalido â†’ 422 â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

    [Fact]
    public async Task Returns422_WhenTrainingTypeIsInvalid()
    {
        var token = await RegisterAndGetTokenAsync("vt-invalid@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();
        await SeedApprovedExerciseAsync();
        var questId = await GenerateQuestAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/validate-training-type-change",
            new { trainingType = "free_edit" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // â"€â"€ RN-001: quest iniciada â†’ 409 â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

    [Fact]
    public async Task RN001_Returns409_WhenQuestAlreadyStarted()
    {
        var token = await RegisterAndGetTokenAsync("vt-started@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();
        await SeedApprovedExerciseAsync();
        var questId = await GenerateQuestAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var quest = await db.Quests.SingleAsync(q => q.Id == questId);
            quest.Start(DateTime.UtcNow, Array.Empty<Awaken.Domain.Entities.Quests.QuestExerciseSeed>());
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/validate-training-type-change",
            new { trainingType = "regeneration" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("QUEST_ALREADY_STARTED");
    }

    // â"€â"€ RN-007: acesso expirado â†’ 403 â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

    [Fact]
    public async Task RN007_Returns403_WhenTrialExpired()
    {
        var token = await RegisterAndGetTokenAsync("vt-expired@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(-1);
        await SeedPaidSubscriptionAsync("vt-expired@awaken.app", "monthly", expiresAt, "rc_vt_expired");

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{Guid.NewGuid()}/validate-training-type-change",
            new { trainingType = "regeneration" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("ACCESS_BLOCKED");
    }

    // â"€â"€ Sem auth â†’ 401 â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

    [Fact]
    public async Task Returns401_WhenUnauthenticated()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{Guid.NewGuid()}/validate-training-type-change",
            new { trainingType = "regeneration" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}


