using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Exercises;
using Awaken.Contracts.Onboarding;
using Awaken.Contracts.Quests;
using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Entities.Progression;
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

public class QuestGenerationEndpointTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("awaken")
        .WithUsername("awaken")
        .WithPassword("awaken_test_password")
        .Build();

    private readonly string _importRootDirectory = Path.Combine(
        Path.GetTempPath(),
        $"awaken-quest-it-{Guid.NewGuid():N}");

    private const string BatchKey = "batch-2026-01";

    private string BatchDirectory => Path.Combine(_importRootDirectory, BatchKey);

    private static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@awaken.app";

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(BatchDirectory);
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                // ExerciseImport:RootDirectory precisa vir daqui (não de builder.UseSetting):
                // appsettings.json define "" explicitamente e é carregado depois dos webHost
                // settings, sobrescrevendo UseSetting de volta para "" (SafeDirectoryResolver
                // sempre resolveria null, e ImportApprovedExerciseAsync cairia em 422).
                // AddInMemoryCollection via ConfigureAppConfiguration é adicionado por último e vence.
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PostgreSQL"] = _postgres.GetConnectionString(),
                    ["ExerciseImport:RootDirectory"] = _importRootDirectory,
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

        if (Directory.Exists(_importRootDirectory))
            Directory.Delete(_importRootDirectory, recursive: true);
    }

    private async Task<string> RegisterAndGetTokenAsync(string email)
    {
        var payload = new { email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
    }

    private async Task<Guid> GetUserIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        return user.Id;
    }

    private async Task StartTrialAsync()
    {
        var response = await _client.PostAsync("/api/subscriptions/trial/start", null);
        response.EnsureSuccessStatusCode();
    }

    private async Task CompleteOnboardingAsync(
        int availableMinutesPerWorkout = 10,
        string experienceLevel = "intermediate",
        string? goal = "gain_muscle",
        string trainingDuration = "1_6_months")
    {
        var payload = new
        {
            goal,
            experienceLevel,
            age = 28,
            heightCm = 175.0,
            weightKg = 82.0,
            biologicalSex = "masculino",
            trainingDuration,
            availableMinutesPerWorkout,
            bodyType = "normal",
            physicalLimitations = new[] { "no_limitations" },
            physicalPains = new[] { "no_pains" }
        };

        var response = await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", payload);
        response.EnsureSuccessStatusCode();
    }

    // Manipulações diretas do ExerciseCatalog no teste (fora do fluxo real de
    // ImportExercisesCommandHandler) precisam invalidar o cache Redis do catálogo
    // aprovado manualmente - senão um snapshot desatualizado (de um teste anterior
    // que rodou contra o mesmo Redis local) pode vazar para este teste.
    private async Task InvalidateApprovedCatalogCacheAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IExerciseCatalogCacheService>();
        await cache.InvalidateApprovedCatalogAsync();
    }

    private async Task SeedApprovedExerciseDirectlyAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        if (await db.ExerciseCatalogs.AnyAsync()) return;
        db.ExerciseCatalogs.Add(BuildApprovedExercise(
            "ex001",
            "Squat",
            "strength",
            primaryMuscleGroups: [MuscleGroups.Quadriceps],
            movementPattern: MovementPatterns.Squat,
            strengthXp: 10));
        await db.SaveChangesAsync();
        await InvalidateApprovedCatalogCacheAsync();
    }

    private async Task ClearExerciseCatalogAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var catalogs = await db.ExerciseCatalogs.ToListAsync();
        if (catalogs.Count > 0)
        {
            db.ExerciseCatalogs.RemoveRange(catalogs);
            await db.SaveChangesAsync();
        }
        await InvalidateApprovedCatalogCacheAsync();
    }

    private async Task ImportApprovedExerciseAsync()
    {
        WriteSampleExercise("0025");

        var response = await _client.PostAsJsonAsync(
            "/api/admin/exercises/import",
            new ImportExercisesRequest(BatchKey, "local_files", approveOnImport: true));

        response.EnsureSuccessStatusCode();
    }

    private static ExerciseCatalog BuildApprovedExercise(
        string providerExerciseId,
        string namePtBr,
        string primaryAttribute,
        List<string>? goalTags = null,
        List<string>? primaryMuscleGroups = null,
        string movementPattern = MovementPatterns.HorizontalPush,
        int difficultyRank = 1,
        bool isWeighted = false,
        int strengthXp = 0,
        int agilityXp = 0,
        int enduranceXp = 0,
        int vitalityXp = 0,
        int focusXp = 0)
    {
        var snapshot = new ExerciseCatalogSnapshot(
            RawImportId: null,
            ProviderName: "test",
            ProviderExerciseId: providerExerciseId,
            ProviderVersion: null,
            NamePtBr: namePtBr,
            NameOriginal: namePtBr,
            Slug: providerExerciseId,
            DescriptionPtBr: "Descricao",
            InstructionsPtBr: ["Passo 1"],
            InstructionsOriginal: ["Step 1"],
            TipsPtBr: [],
            ExerciseType: "strength",
            MovementPattern: movementPattern,
            MovementFamily: "family",
            Mechanic: "compound",
            ForceType: "push",
            PlaneOfMotion: "sagittal",
            Laterality: "bilateral",
            BodyPosition: "standing",
            BenchAngle: null,
            EquipmentCategory: "bodyweight",
            LoadType: "bodyweight",
            PrimaryRegion: "upper_body",
            DifficultyLevel: "intermediate",
            DifficultyRank: difficultyRank,
            TechnicalComplexity: 1,
            ImpactLevel: 1,
            Environment: "home",
            RequiredEquipment: [],
            PrimaryMuscleGroups: primaryMuscleGroups ?? ["chest"],
            SecondaryMuscleGroups: [],
            BodyParts: ["chest"],
            JointStressTags: [],
            ContraindicationTags: [],
            LimitationBlockTags: [],
            PainBlockTags: [],
            GoalTags: goalTags ?? ["strength"],
            RiskTags: [],
            AccessibilityTags: [],
            TaxonomySignals: [],
            MinExperienceLevel: "beginner",
            SuitableForSedentary: true,
            SuitableForBeginner: true,
            SuitableForIntermediate: true,
            SuitableForAdvanced: true,
            IsCompound: false,
            IsUnilateral: false,
            IsAssisted: false,
            IsWeighted: isWeighted,
            RegressionExerciseIds: [],
            ProgressionExerciseIds: [],
            RelatedExerciseIds: [],
            VideoUrl: "https://video.example/" + providerExerciseId,
            ImageUrl: null,
            GifUrl: null,
            MediaLicenseInfo: null,
            SanitizationStatus: "approved",
            IsApprovedForWorkoutGeneration: true,
            Confidence: "high");

        var exercise = ExerciseCatalog.Create(snapshot, DateTime.UtcNow);
        exercise.SetAttributeContribution(ExerciseAttributeContribution.CreateAutoGenerated(
            primaryAttribute,
            strengthXp,
            agilityXp,
            enduranceXp,
            vitalityXp,
            focusXp,
            wisdomXp: 1), DateTime.UtcNow);

        return exercise;
    }

    private async Task SeedProgressionAsync(string email, int strength, int agility, int endurance, int vitality, int focus)
    {
        var userId = await GetUserIdAsync(email);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var existing = await dbContext.HunterProgressions.SingleOrDefaultAsync(p => p.UserId == userId);
        if (existing is not null)
            dbContext.HunterProgressions.Remove(existing);

        dbContext.HunterProgressions.Add(HunterProgression.CreateFromOnboarding(
            userId,
            strength: strength,
            agility: agility,
            endurance: endurance,
            vitality: vitality,
            focus: focus,
            wisdom: 1));

        await dbContext.SaveChangesAsync();
    }

    private async Task SeedAttributeDrivenExercisesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        dbContext.ExerciseCatalogs.AddRange(
            BuildApprovedExercise(
                providerExerciseId: "strength_focus",
                namePtBr: "Pressao de Forca",
                primaryAttribute: "strength",
                goalTags: ["strength"],
                primaryMuscleGroups: [MuscleGroups.Chest],
                movementPattern: MovementPatterns.HorizontalPush,
                strengthXp: 10),
            BuildApprovedExercise(
                providerExerciseId: "endurance_focus",
                namePtBr: "Mobilidade de Resistencia",
                primaryAttribute: "agility",
                goalTags: ["conditioning"],
                primaryMuscleGroups: [MuscleGroups.Quadriceps],
                movementPattern: MovementPatterns.Squat,
                difficultyRank: 3,
                isWeighted: true,
                enduranceXp: 10),
            BuildApprovedExercise(
                providerExerciseId: "agility_focus",
                namePtBr: "Remada de Agilidade",
                primaryAttribute: "agility",
                goalTags: ["conditioning"],
                primaryMuscleGroups: [MuscleGroups.Back],
                movementPattern: MovementPatterns.HorizontalPull,
                agilityXp: 10),
            BuildApprovedExercise(
                providerExerciseId: "vitality_focus",
                namePtBr: "Desenvolvimento de Vitalidade",
                primaryAttribute: "vitality",
                primaryMuscleGroups: [MuscleGroups.Shoulders],
                movementPattern: MovementPatterns.VerticalPush,
                vitalityXp: 10),
            BuildApprovedExercise(
                providerExerciseId: "focus_core",
                namePtBr: "Abdominal de Foco",
                primaryAttribute: "focus",
                primaryMuscleGroups: [MuscleGroups.Core],
                movementPattern: MovementPatterns.CoreFlexion,
                focusXp: 10));

        await dbContext.SaveChangesAsync();
        await InvalidateApprovedCatalogCacheAsync();
    }

    private void WriteSampleExercise(string id)
    {
        File.WriteAllText(Path.Combine(BatchDirectory, $"{id}.json"), SampleExerciseJson(id));
        File.WriteAllBytes(Path.Combine(BatchDirectory, $"{id}-360.gif"), [71, 73, 70, 56]);
    }

    private static string SampleExerciseJson(string id) => $$"""
    {
      "bodyPart": "chest",
      "equipment": "body_weight",
      "id": "{{id}}",
      "name": "barbell bench press",
      "target": "pectorals",
      "secondaryMuscles": ["triceps", "shoulders"],
      "instructions": ["Lie flat on a bench.", "Press the barbell up."],
      "description": "Classic compound chest exercise.",
      "difficulty": "intermediate",
      "category": "strength",
      "taxonomy": {
        "movementFamily": "bench press",
        "movementPattern": "horizontal push",
        "mechanic": "compound",
        "forceType": "push",
        "planeOfMotion": "sagittal",
        "laterality": "bilateral",
        "bodyPosition": "lying",
        "benchAngle": "flat",
        "equipmentCategory": "free_weight",
        "loadType": "free_weight",
        "primaryRegion": "upper_body",
        "isCompound": true,
        "isUnilateral": false,
        "isAssisted": false,
        "isWeighted": true,
        "signals": ["external_load", "free_weight"],
        "confidence": "high"
      },
      "similarExercises": [
        { "id": "0033", "name": "barbell decline bench press", "score": 100.0, "confidence": "high", "reasons": ["same target muscle"] }
      ],
      "substitutions": [
        { "id": "0289", "name": "dumbbell bench press", "types": ["equipment_alternative"], "score": 100.0, "confidence": "high", "reasons": ["different equipment option"] }
      ],
      "progressions": [
        { "id": "0045", "name": "barbell guillotine bench press", "types": ["higher_difficulty"], "score": 100.0, "confidence": "high", "reasons": ["advanced variant"] }
      ],
      "regressions": [
        { "id": "0748", "name": "smith bench press", "types": ["lower_difficulty"], "score": 100.0, "confidence": "high", "reasons": ["beginner variant"] }
      ]
    }
    """;

    [Fact]
    public async Task CA001_BeginnerGetsFixedReps_RepsMaxIsNull()
    {
        var email = UniqueEmail("quest-ca001-beginner");
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync(availableMinutesPerWorkout: 30, experienceLevel: "beginner", goal: "gain_muscle");
        await ClearExerciseCatalogAsync();
        await SeedApprovedExerciseDirectlyAsync();

        var response = await _client.PostAsync("/api/quests/daily/generate", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<QuestResponse>();
        body!.Workout.Should().NotBeNull();
        var exercise = body.Workout!.Exercises.First();
        exercise.RepsMax.Should().BeNull("beginners get fixed reps per RN-007");
        exercise.RepsMin.Should().BeInRange(8, 15, "beginner rep band RN-002");
        exercise.Sets.Should().BeInRange(2, 3, "beginner sets band RN-002");
    }

    [Fact]
    public async Task CA003_IntermediateGetsRepRange_RepsMaxIsGreaterThanRepsMin()
    {
        var email = UniqueEmail("quest-ca003-intermediate");
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        // trainingDuration "6_12_months" → implied level = intermediate (ExperienceLevelCalculator)
        await CompleteOnboardingAsync(availableMinutesPerWorkout: 30, experienceLevel: "intermediate", goal: "gain_muscle", trainingDuration: "6_12_months");
        await ClearExerciseCatalogAsync();
        await SeedApprovedExerciseDirectlyAsync();

        var response = await _client.PostAsync("/api/quests/daily/generate", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<QuestResponse>();
        body!.Workout.Should().NotBeNull();
        var exercise = body.Workout!.Exercises.First();
        exercise.RepsMax.Should().NotBeNull("intermediate users get a rep range per CA-003/RN-007");
        exercise.RepsMax!.Should().BeGreaterThan(exercise.RepsMin);
        exercise.RepsMin.Should().BeInRange(10, 20, "intermediate rep band RN-003");
        exercise.RepsMax.Should().BeInRange(10, 20, "intermediate rep band RN-003");
    }

    [Fact]
    public async Task CA004_SedentaryGetsFixedReps_RepsMaxIsNull()
    {
        var email = UniqueEmail("quest-ca004-sedentary");
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        // trainingDuration "does_not_train" → implied level = sedentary; goal must be a valid enum value
        await CompleteOnboardingAsync(availableMinutesPerWorkout: 30, experienceLevel: "sedentary", goal: "stay_active", trainingDuration: "does_not_train");
        await ClearExerciseCatalogAsync();
        await SeedApprovedExerciseDirectlyAsync();

        var response = await _client.PostAsync("/api/quests/daily/generate", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<QuestResponse>();
        body!.Workout.Should().NotBeNull();
        var exercise = body.Workout!.Exercises.First();
        exercise.RepsMax.Should().BeNull("sedentary users get fixed reps per CA-004/RN-007");
        exercise.RepsMin.Should().BeInRange(6, 12, "sedentary rep band RN-001");
    }

    [Fact]
    public async Task CA005_AdvancedGetsRepRange_AndGoalAdjustsRestAndIntensity()
    {
        var email = UniqueEmail("quest-ca005-advanced");
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await StartTrialAsync();
        await CompleteOnboardingAsync(
            availableMinutesPerWorkout: 30,
            experienceLevel: "advanced",
            goal: "gain_strength",
            trainingDuration: "more_than_3_years");
        await ClearExerciseCatalogAsync();
        await SeedApprovedExerciseDirectlyAsync();

        var response = await _client.PostAsync("/api/quests/daily/generate", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<QuestResponse>();
        body!.Workout.Should().NotBeNull();
        // SeedApprovedExerciseDirectlyAsync só semeia "Squat" (ex001) - "Pressao de Forca" é
        // de SeedAttributeDrivenExercisesAsync (usado por outros testes), nunca chamado aqui.
        var exercise = body.Workout!.Exercises.First();
        exercise.RepsMax.Should().NotBeNull("advanced users get a rep range per CA-003/RN-007");
        exercise.RepsMin.Should().BeInRange(4, 30, "advanced rep band RN-004");
        exercise.RepsMax!.Should().BeGreaterThan(exercise.RepsMin);
        exercise.Sets.Should().Be(5);
        exercise.RestSeconds.Should().Be(180);
        exercise.TargetRpe.Should().Be("8-9");
    }

    [Fact]
    public async Task US046_FallbackTemplate_UsesPrescriptionWhenNoEligibleExercisesExist()
    {
        var email = UniqueEmail("quest-us046-fallback");
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await StartTrialAsync();
        await ClearExerciseCatalogAsync();
        await CompleteOnboardingAsync(
            availableMinutesPerWorkout: 30,
            experienceLevel: "beginner",
            goal: "gain_muscle",
            trainingDuration: "1_6_months");

        var response = await _client.PostAsync("/api/quests/daily/generate", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<QuestResponse>();
        body!.IsPersonalized.Should().BeFalse();
        body.Workout.Should().NotBeNull();
        body.Workout!.Exercises.Should().HaveCount(3);

        var exercise = body.Workout.Exercises.First();
        exercise.RepsMax.Should().BeNull();
        exercise.RepsMin.Should().Be(12);
        exercise.Sets.Should().Be(3);
        exercise.RestSeconds.Should().Be(60);
        exercise.TargetRpe.Should().Be("5-6");
    }

    [Fact]
    public async Task US152_GeneratingDailyQuest_PrioritizesLowStrengthExerciseOverUnrelatedAlternative()
    {
        var email = UniqueEmail("quest-us152");
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await StartTrialAsync();
        await CompleteOnboardingAsync(
            availableMinutesPerWorkout: 10,
            trainingDuration: "6_12_months");
        await ClearExerciseCatalogAsync();
        await SeedProgressionAsync(email, strength: 1, agility: 10, endurance: 5, vitality: 5, focus: 5);
        await SeedAttributeDrivenExercisesAsync();

        var response = await _client.PostAsync("/api/quests/daily/generate", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var quest = await response.Content.ReadFromJsonAsync<QuestResponse>();
        quest!.Workout!.Exercises.Should().NotBeEmpty();
        quest.IsPersonalized.Should().BeTrue();

        var exercise = quest.Workout.Exercises.First();
        exercise.Name.Should().Be("Pressao de Forca");
        // US-242: orçamento de tempo determinístico - com só 10 min disponíveis, 4 séries de
        // 10-15 reps + 90s de descanso não cabem (RN-002); o ladder reduz para 3 séries antes
        // de tocar no descanso do objetivo (RN-004), preservando RestSeconds/TargetRpe.
        exercise.Sets.Should().Be(3);
        exercise.RepsMin.Should().Be(10);
        exercise.RepsMax.Should().Be(15);
        exercise.RestSeconds.Should().Be(90);
        exercise.TargetRpe.Should().Be("6-8");
    }

    [Fact]
    public async Task GenerateTodayAndConfirmQuest_WithActiveAccess_PersistsAndConfirmsDailyQuest()
    {
        var token = await RegisterAndGetTokenAsync(UniqueEmail("quest-active"));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await StartTrialAsync();
        await ClearExerciseCatalogAsync();
        await SeedApprovedExerciseDirectlyAsync();
        await CompleteOnboardingAsync();

        var generateResponse = await _client.PostAsync("/api/quests/daily/generate", null);
        generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var generated = await generateResponse.Content.ReadFromJsonAsync<QuestResponse>();
        generated!.Type.Should().Be("daily");
        generated.IsConfirmed.Should().BeFalse();
        generated.Workout!.DurationMinutes.Should().Be(10);
        generated.Workout.Exercises.Should().HaveCount(1);

        var todayResponse = await _client.GetAsync("/api/quests/daily/today");
        todayResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var today = await todayResponse.Content.ReadFromJsonAsync<QuestResponse>();
        today!.Id.Should().Be(generated.Id);
        today.IsConfirmed.Should().BeFalse();

        var confirmResponse = await _client.PostAsync($"/api/quests/daily/{generated.Id}/confirm", null);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<QuestResponse>();
        confirmed!.Id.Should().Be(generated.Id);
        confirmed.IsConfirmed.Should().BeTrue();

        var refreshedToday = await _client.GetAsync("/api/quests/daily/today");
        refreshedToday.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await refreshedToday.Content.ReadFromJsonAsync<QuestResponse>();
        refreshed!.Id.Should().Be(generated.Id);
        refreshed.IsConfirmed.Should().BeTrue();
    }
}
