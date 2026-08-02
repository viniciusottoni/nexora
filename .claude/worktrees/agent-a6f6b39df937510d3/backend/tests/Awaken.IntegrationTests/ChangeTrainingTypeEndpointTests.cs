﻿// US-051 / US-052 / US-055: troca de tipo de treino antes de iniciar a quest;
// bloqueio de ediÃ§Ã£o manual de exercÃ­cios; bloqueio para acesso expirado.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Quests;
using Awaken.Domain.Entities.Exercises;
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

public class ChangeTrainingTypeEndpointTests : IAsyncLifetime
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
            RawImportId: null, ProviderName: "test", ProviderExerciseId: "change-type-ex001",
            ProviderVersion: null, NamePtBr: "Squat", NameOriginal: "Squat", Slug: "change-type-ex001",
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
            VideoUrl: "https://video.example/change-type-ex001", ImageUrl: null, GifUrl: null,
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

    // â"€â"€ CA-001 (US-051): alterar para regeneraÃ§Ã£o â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

    [Fact]
    public async Task CA001_Returns200_WhenChangingToRegeneration()
    {
        var token = await RegisterAndGetTokenAsync("ct-regen@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();
        await SeedApprovedExerciseAsync();
        var questId = await GenerateQuestAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/quests/{questId}/training-type",
            new { trainingType = "regeneration" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await response.Content.ReadFromJsonAsync<QuestPreviewResponse>();
        preview.Should().NotBeNull();
        preview!.TrainingType.Should().Be("regeneration");
        preview.CanChangeTrainingType.Should().BeTrue();
        preview.Workout.Should().NotBeNull();
    }

    // â"€â"€ CA-002 (US-051): alterar para Caminho de Saitama â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

    [Fact]
    public async Task CA002_Returns200_WhenChangingToSaitamaPath()
    {
        var token = await RegisterAndGetTokenAsync("ct-saitama@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();
        await SeedApprovedExerciseAsync();
        var questId = await GenerateQuestAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/quests/{questId}/training-type",
            new { trainingType = "program", programId = "saitama_path" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await response.Content.ReadFromJsonAsync<QuestPreviewResponse>();
        preview!.TrainingType.Should().Be("program");
        preview.Workout!.Title.Should().Contain("Saitama");
    }

    // â"€â"€ CA-002 (US-051): alterar para Perfect 2 â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

    [Fact]
    public async Task CA002_Returns200_WhenChangingToPerfect2()
    {
        var token = await RegisterAndGetTokenAsync("ct-perfect2@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();
        await SeedApprovedExerciseAsync();
        var questId = await GenerateQuestAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/quests/{questId}/training-type",
            new { trainingType = "program", programId = "perfect_2" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await response.Content.ReadFromJsonAsync<QuestPreviewResponse>();
        preview!.TrainingType.Should().Be("program");
        preview.Workout!.Title.Should().Contain("Perfect 2");
    }

    // â"€â"€ RN-001 (US-051): bloqueio apÃ³s quest iniciada â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

    [Fact]
    public async Task RN001_Returns409_WhenQuestAlreadyStarted()
    {
        var token = await RegisterAndGetTokenAsync("ct-started@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();
        await SeedApprovedExerciseAsync();
        var questId = await GenerateQuestAsync();

        // Marca quest como in_progress direto no banco
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var quest = await db.Quests.SingleAsync(q => q.Id == questId);
        quest.Start(DateTime.UtcNow, Array.Empty<Awaken.Domain.Entities.Quests.QuestExerciseSeed>());
        await db.SaveChangesAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/quests/{questId}/training-type",
            new { trainingType = "regeneration" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("QUEST_ALREADY_STARTED");
    }

    // â"€â"€ RN-001 (US-051): validaÃ§Ã£o de tipo invÃ¡lido â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

    [Fact]
    public async Task Returns422_WhenTrainingTypeIsInvalid()
    {
        var token = await RegisterAndGetTokenAsync("ct-invalid@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();
        await SeedApprovedExerciseAsync();
        var questId = await GenerateQuestAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/quests/{questId}/training-type",
            new { trainingType = "free_edit" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // â"€â"€ US-052: bloqueio de ediÃ§Ã£o manual via endpoint â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

    [Fact]
    public async Task CA002_Returns409_WhenAttemptingManualExerciseEdit()
    {
        var token = await RegisterAndGetTokenAsync("ct-manualedit@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();

        var fakeQuestId = Guid.NewGuid();
        var response = await _client.PatchAsJsonAsync(
            $"/api/quests/{fakeQuestId}/exercises/some-exercise-id",
            new { sets = 5 });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("MANUAL_WORKOUT_EDIT_NOT_ALLOWED");
    }

    // â"€â"€ US-055: acesso expirado bloqueado pelo middleware â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

    [Fact]
    public async Task CA001_Returns403_WhenTrialExpired()
    {
        var token = await RegisterAndGetTokenAsync("ct-expired@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Simula assinatura expirada (sem trial)
        var expiresAt = DateTime.UtcNow.AddDays(-1);
        await SeedPaidSubscriptionAsync("ct-expired@awaken.app", "monthly", expiresAt, "rc_ct_expired");

        var response = await _client.PatchAsJsonAsync(
            $"/api/quests/{Guid.NewGuid()}/training-type",
            new { trainingType = "regeneration" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("ACCESS_BLOCKED");
    }

    // â"€â"€ Auth: unauthenticated â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

    [Fact]
    public async Task Returns401_WhenUnauthenticated()
    {
        var response = await _client.PatchAsJsonAsync(
            $"/api/quests/{Guid.NewGuid()}/training-type",
            new { trainingType = "regeneration" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}


