// US-048/US-049 + obs do "Pergaminho da Reforja": regeneracao da quest diaria
// respeita o limite diario (RN-001/RN-003) e, esgotado o limite, so e possivel
// regenerar consumindo o item via loja/inventario.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Common;
using Awaken.Contracts.Exercises;
using Awaken.Contracts.Inventory;
using Awaken.Contracts.Onboarding;
using Awaken.Contracts.Quests;
using Awaken.Contracts.Shop;
using Awaken.Domain.Services.Quests;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class QuestRegenerationEndpointTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("awaken")
        .WithUsername("awaken")
        .WithPassword("awaken_test_password")
        .Build();

    private readonly string _importRootDirectory = Path.Combine(
        Path.GetTempPath(),
        $"awaken-quest-regen-it-{Guid.NewGuid():N}");

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

    private async Task<string> RegisterAdminAndGetTokenAsync(string email)
    {
        await RegisterAndGetTokenAsync(email);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            "UPDATE users SET \"Role\" = 'Admin' WHERE \"Email\" = {0}",
            email);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Str0ngPass!"
        });
        loginResponse.EnsureSuccessStatusCode();
        return (await loginResponse.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
    }

    private async Task<Guid> GetUserIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        return (await dbContext.Users.SingleAsync(u => u.Email == email)).Id;
    }

    private async Task SeedGoldBalanceAsync(Guid userId, long amount)
    {
        using var scope = _factory.Services.CreateScope();
        var walletService = scope.ServiceProvider.GetRequiredService<IGoldWalletService>();
        await walletService.CreditAsync(userId, amount, "test_setup");
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
            trainingDuration = "1_6_months",
            availableMinutesPerWorkout = 30,
            bodyType = "normal",
            physicalLimitations = new[] { "no_limitations" },
            physicalPains = new[] { "no_pains" }
        };

        var response = await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", payload);
        response.EnsureSuccessStatusCode();
    }

    private async Task ImportApprovedExerciseAsync()
    {
        File.WriteAllText(Path.Combine(BatchDirectory, "0025.json"), SampleExerciseJson("0025"));
        File.WriteAllBytes(Path.Combine(BatchDirectory, "0025-360.gif"), [71, 73, 70, 56]);

        var response = await _client.PostAsJsonAsync(
            "/api/admin/exercises/import",
            new ImportExercisesRequest(BatchKey, "local_files", approveOnImport: true));

        response.EnsureSuccessStatusCode();
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
      "similarExercises": [],
      "substitutions": [],
      "progressions": [],
      "regressions": []
    }
    """;

    private async Task AuthenticateNewHunterAsync(string email)
    {
        var token = await RegisterAdminAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await ImportApprovedExerciseAsync();
        await CompleteOnboardingAsync();
    }

    [Fact]
    public async Task CA001_RegenerateDaily_WithinLimit_ReturnsNewQuest_AndIncrementsCounter()
    {
        await AuthenticateNewHunterAsync(UniqueEmail("quest_regen_within_limit"));
        await _client.PostAsync("/api/quests/daily/generate", null);

        var response = await _client.PostAsJsonAsync(
            "/api/quests/daily/regenerate", new RegenerateDailyQuestRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var quest = await response.Content.ReadFromJsonAsync<QuestResponse>();
        quest!.RegenerationsUsed.Should().Be(1);
    }

    [Fact]
    public async Task CA002_RegenerateDaily_BeyondLimit_WithoutScroll_Returns409()
    {
        await AuthenticateNewHunterAsync(UniqueEmail("quest_regen_limit_reached"));
        await _client.PostAsync("/api/quests/daily/generate", null);

        for (var i = 0; i < QuestRegenerationPolicy.DailyFreeLimit; i++)
        {
            var ok = await _client.PostAsJsonAsync("/api/quests/daily/regenerate", new RegenerateDailyQuestRequest());
            ok.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var blocked = await _client.PostAsJsonAsync("/api/quests/daily/regenerate", new RegenerateDailyQuestRequest());

        blocked.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await blocked.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("REGENERATION_LIMIT_REACHED");
    }

    [Fact]
    public async Task Obs_PurchaseReforgeScroll_ThenRegenerateBeyondLimit_ConsumesOneUnit()
    {
        var email = UniqueEmail("quest_regen_reforge_scroll");
        await AuthenticateNewHunterAsync(email);
        await SeedGoldBalanceAsync(await GetUserIdAsync(email), 200);
        await _client.PostAsync("/api/quests/daily/generate", null);

        for (var i = 0; i < QuestRegenerationPolicy.DailyFreeLimit; i++)
        {
            await _client.PostAsJsonAsync("/api/quests/daily/regenerate", new RegenerateDailyQuestRequest());
        }

        var noScroll = await _client.GetAsync("/api/inventory/items/reforja_scroll");
        (await noScroll.Content.ReadFromJsonAsync<InventoryItemResponse>())!.Quantity.Should().Be(0);

        var purchase = await _client.PostAsync("/api/shop/items/reforja_scroll/purchase", null);
        purchase.StatusCode.Should().Be(HttpStatusCode.OK);
        (await purchase.Content.ReadFromJsonAsync<ApiResponse<ShopOrderResponse>>())!
            .Data.Status.Should().Be("granted");

        var afterPurchase = await _client.GetAsync("/api/inventory/items/reforja_scroll");
        (await afterPurchase.Content.ReadFromJsonAsync<InventoryItemResponse>())!.Quantity.Should().Be(1);

        var regenerated = await _client.PostAsJsonAsync(
            "/api/quests/daily/regenerate", new RegenerateDailyQuestRequest(UseReforgeScroll: true));
        regenerated.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterConsume = await _client.GetAsync("/api/inventory/items/reforja_scroll");
        (await afterConsume.Content.ReadFromJsonAsync<InventoryItemResponse>())!.Quantity.Should().Be(0);
    }

    [Fact]
    public async Task GetShopItems_IncludesReforgeScroll()
    {
        await AuthenticateNewHunterAsync(UniqueEmail("quest_shop_items"));

        var response = await _client.GetAsync("/api/shop/items");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<ShopItemResponse>>();
        items.Should().Contain(i => i.ItemKey == "reforja_scroll");
    }
}
