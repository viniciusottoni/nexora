// US-209: testes de integracao para paginacao por cursor do historico de batalha.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.BattleLog;
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

public class BattleLogCursorPaginationTests : IAsyncLifetime
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

    private async Task AuthenticateNewHunterAsync(string email)
    {
        var token = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();
    }

    private async Task<Guid> SeedQuestLogAsync(string email, string questType, long xpEarned, DateTime completedAt)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        var log = QuestLog.Create(
            questId: Guid.NewGuid(),
            userId: user.Id,
            questType: questType,
            xpEarned: xpEarned,
            strengthXpEarned: 0,
            agilityXpEarned: 0,
            enduranceXpEarned: 0,
            vitalityXpEarned: 0,
            focusXpEarned: 0,
            wisdomXpEarned: 0,
            strengthPointsGranted: 0,
            agilityPointsGranted: 0,
            endurancePointsGranted: 0,
            vitalityPointsGranted: 0,
            focusPointsGranted: 0,
            itemsEarned: [],
            completedAtUtc: completedAt);

        dbContext.QuestLogs.Add(log);
        await dbContext.SaveChangesAsync();
        return log.Id;
    }

    // ─── US-209 CA001: primeira pagina retorna itens e nextCursor quando hasMore ──

    [Fact]
    public async Task CA001_FirstPage_ReturnsItemsAndNextCursor_WhenHasMore()
    {
        const string email = "cursor_ca001@awaken.app";
        await AuthenticateNewHunterAsync(email);

        // Seed 5 quests para paginar com limit=3
        for (var i = 0; i < 5; i++)
            await SeedQuestLogAsync(email, "daily", 50 + i, DateTime.UtcNow.AddMinutes(-i));

        var response = await _client.GetAsync("/api/hunter/battle-log/cursor?limit=3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CursorPagedResponse<BattleLogItemResponse>>();

        result!.Items.Should().HaveCount(3);
        result.HasMore.Should().BeTrue();
        result.NextCursor.Should().NotBeNullOrEmpty("cursor e necessario para buscar proxima pagina");
    }

    // ─── US-209 CA002: usar nextCursor retorna a proxima pagina ─────────────

    [Fact]
    public async Task CA002_UsingNextCursor_ReturnsContinuationPage()
    {
        const string email = "cursor_ca002@awaken.app";
        await AuthenticateNewHunterAsync(email);

        // Seed 5 quests
        for (var i = 0; i < 5; i++)
            await SeedQuestLogAsync(email, "daily", 50 + i, DateTime.UtcNow.AddMinutes(-i));

        // Primeira pagina
        var firstResponse = await _client.GetAsync("/api/hunter/battle-log/cursor?limit=3");
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstPage = await firstResponse.Content.ReadFromJsonAsync<CursorPagedResponse<BattleLogItemResponse>>();
        firstPage!.HasMore.Should().BeTrue();
        var cursor = firstPage.NextCursor;

        // Segunda pagina usando o cursor
        var secondResponse = await _client.GetAsync($"/api/hunter/battle-log/cursor?limit=3&cursor={cursor}");
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondPage = await secondResponse.Content.ReadFromJsonAsync<CursorPagedResponse<BattleLogItemResponse>>();

        secondPage!.Items.Should().HaveCount(2, "restam apenas 2 itens apos a primeira pagina");
        secondPage.HasMore.Should().BeFalse();
        secondPage.NextCursor.Should().BeNull("nao ha mais paginas");

        // Itens nao se repetem entre paginas
        var firstIds = firstPage.Items.Select(i => i.QuestLogId).ToHashSet();
        secondPage.Items.Should().NotContain(i => firstIds.Contains(i.QuestLogId));
    }

    // ─── US-209 CA003: cursor invalido retorna pagina vazia (comportamento controlado) ──

    [Fact]
    public async Task CA003_InvalidCursor_ReturnsFirstPage_OrEmpty()
    {
        const string email = "cursor_ca003@awaken.app";
        await AuthenticateNewHunterAsync(email);

        await SeedQuestLogAsync(email, "daily", 80, DateTime.UtcNow);

        // Cursor invalido (nao e um Guid) — deve ser ignorado e retornar a primeira pagina
        var response = await _client.GetAsync("/api/hunter/battle-log/cursor?cursor=nao_e_um_guid&limit=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "cursor invalido deve ser tratado graciosamente");

        var result = await response.Content.ReadFromJsonAsync<CursorPagedResponse<BattleLogItemResponse>>();
        result!.Items.Should().HaveCount(1, "cursor invalido e ignorado, retorna primeira pagina");
    }

    // ─── US-209 RN: limit capeado a 50 ────────────────────────────────────

    [Fact]
    public async Task RN_LimitCappedAt50_Returns400_WhenExceeded()
    {
        const string email = "cursor_limit@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var response = await _client.GetAsync("/api/hunter/battle-log/cursor?limit=100");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "limit > 50 deve retornar 400");
    }

    // ─── US-209 RN: limit invalido (0 ou negativo) → 400 ─────────────────

    [Fact]
    public async Task RN_LimitZeroOrNegative_Returns400()
    {
        const string email = "cursor_zero@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var response = await _client.GetAsync("/api/hunter/battle-log/cursor?limit=0");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── US-209 RN: ultima pagina sem nextCursor ────────────────────────────

    [Fact]
    public async Task RN_LastPage_HasMoreFalseAndNullNextCursor()
    {
        const string email = "cursor_lastpage@awaken.app";
        await AuthenticateNewHunterAsync(email);

        for (var i = 0; i < 3; i++)
            await SeedQuestLogAsync(email, "daily", 50 + i, DateTime.UtcNow.AddMinutes(-i));

        var response = await _client.GetAsync("/api/hunter/battle-log/cursor?limit=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CursorPagedResponse<BattleLogItemResponse>>();

        result!.Items.Should().HaveCount(3);
        result.HasMore.Should().BeFalse();
        result.NextCursor.Should().BeNull();
    }

    // ─── Unauthenticated → 401 ──────────────────────────────────────────────

    [Fact]
    public async Task Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/hunter/battle-log/cursor");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
