using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Quests;
using Awaken.Domain.Entities.Exercises;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class QuestPreviewEndpointTests : IAsyncLifetime
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
            RawImportId: null,
            ProviderName: "test",
            ProviderExerciseId: "preview-ex001",
            ProviderVersion: null,
            NamePtBr: "Squat",
            NameOriginal: "Squat",
            Slug: "preview-ex001",
            DescriptionPtBr: "Agachamento",
            InstructionsPtBr: ["DesÃ§a devagar"],
            InstructionsOriginal: ["Go down slowly"],
            TipsPtBr: [],
            ExerciseType: "strength",
            MovementPattern: "squat",
            MovementFamily: "legs",
            Mechanic: "compound",
            ForceType: "push",
            PlaneOfMotion: "sagittal",
            Laterality: "bilateral",
            BodyPosition: "standing",
            BenchAngle: null,
            EquipmentCategory: "bodyweight",
            LoadType: "bodyweight",
            PrimaryRegion: "lower_body",
            DifficultyLevel: "intermediate",
            DifficultyRank: 2,
            TechnicalComplexity: 2,
            ImpactLevel: 2,
            Environment: "home",
            RequiredEquipment: [],
            PrimaryMuscleGroups: ["quadriceps"],
            SecondaryMuscleGroups: ["glutes"],
            BodyParts: ["legs"],
            JointStressTags: [],
            ContraindicationTags: [],
            LimitationBlockTags: [],
            PainBlockTags: [],
            GoalTags: ["gain_muscle", "strength"],
            RiskTags: [],
            AccessibilityTags: [],
            TaxonomySignals: [],
            MinExperienceLevel: "beginner",
            SuitableForSedentary: true,
            SuitableForBeginner: true,
            SuitableForIntermediate: true,
            SuitableForAdvanced: true,
            IsCompound: true,
            IsUnilateral: false,
            IsAssisted: false,
            IsWeighted: false,
            RegressionExerciseIds: [],
            ProgressionExerciseIds: [],
            RelatedExerciseIds: [],
            VideoUrl: "https://video.example/preview-ex001",
            ImageUrl: null,
            GifUrl: null,
            MediaLicenseInfo: null,
            SanitizationStatus: "approved",
            IsApprovedForWorkoutGeneration: true,
            Confidence: "high");

        var exercise = ExerciseCatalog.Create(snapshot, DateTime.UtcNow);
        exercise.SetAttributeContribution(ExerciseAttributeContribution.CreateAutoGenerated(
            primaryAttribute: "strength",
            strengthXp: 10,
            agilityXp: 0,
            enduranceXp: 0,
            vitalityXp: 0,
            focusXp: 0,
            wisdomXp: 1), DateTime.UtcNow);

        db.ExerciseCatalogs.Add(exercise);
        await db.SaveChangesAsync();
    }

    private async Task<Guid> GenerateQuestAndGetIdAsync()
    {
        var response = await _client.PostAsync("/api/quests/daily/generate", null);
        response.EnsureSuccessStatusCode();
        var quest = await response.Content.ReadFromJsonAsync<QuestResponse>();
        return quest!.Id;
    }

    [Fact]
    public async Task CA001_Returns200_WithPreviewData_WhenQuestExists()
    {
        var token = await RegisterAndGetTokenAsync("preview-ok@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();
        await SeedApprovedExerciseAsync();

        var questId = await GenerateQuestAndGetIdAsync();

        var response = await _client.GetAsync($"/api/quests/{questId}/preview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await response.Content.ReadFromJsonAsync<QuestPreviewResponse>();
        preview.Should().NotBeNull();
        preview!.QuestId.Should().Be(questId);
        preview.QuestType.Should().Be("daily");
        preview.TrainingType.Should().Be("program");
        preview.CanChangeTrainingType.Should().BeTrue();
        preview.EstimatedDurationMinutes.Should().BeGreaterThan(0);
        preview.EstimatedXp.Should().BeGreaterThan(0);
        preview.Workout.Should().NotBeNull();
        preview.Workout!.Exercises.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RN001_Returns401_WhenUnauthenticated()
    {
        var response = await _client.GetAsync($"/api/quests/{Guid.NewGuid()}/preview");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RN001_Returns404_WhenQuestDoesNotExist()
    {
        var token = await RegisterAndGetTokenAsync("preview-notfound@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();

        var response = await _client.GetAsync($"/api/quests/{Guid.NewGuid()}/preview");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RN001_Returns401_WhenUserTriesToViewAnotherUsersQuest()
    {
        // User A gera uma quest
        var tokenA = await RegisterAndGetTokenAsync("preview-userA@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        await StartTrialAsync();
        await CompleteOnboardingAsync();
        await SeedApprovedExerciseAsync();
        var questId = await GenerateQuestAndGetIdAsync();

        // User B tenta acessar a quest de User A
        var tokenB = await RegisterAndGetTokenAsync("preview-userB@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        await _client.PostAsync("/api/subscriptions/trial/start", null);

        var response = await _client.GetAsync($"/api/quests/{questId}/preview");

        // UnauthorizedException â†’ 401 (ExceptionHandlingMiddleware)
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}


