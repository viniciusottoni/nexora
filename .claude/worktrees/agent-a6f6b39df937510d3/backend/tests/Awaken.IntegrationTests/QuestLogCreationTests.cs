// US-085: Registrar logs de conclusão de quest — testes de integração.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.BattleLog;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class QuestLogCreationTests : IAsyncLifetime
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

    private async Task AuthenticateNewHunterAsync(string email)
    {
        var payload = new { email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR" };
        var reg = await _client.PostAsJsonAsync("/api/auth/register", payload);
        reg.EnsureSuccessStatusCode();
        var token = (await reg.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        (await _client.PostAsync("/api/subscriptions/trial/start", null)).EnsureSuccessStatusCode();

        var onboarding = new
        {
            goal = "gain_muscle", experienceLevel = "intermediate", age = 28,
            heightCm = 175.0, weightKg = 82.0, biologicalSex = "masculino",
            trainingDuration = "6_12_months", availableMinutesPerWorkout = 30,
            bodyType = "normal", physicalLimitations = new[] { "no_limitations" },
            physicalPains = new[] { "no_pains" }
        };
        (await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", onboarding))
            .EnsureSuccessStatusCode();
    }

    // ─── CA-001: log criado ─────────────────────────────────────────────────

    [Fact]
    public async Task CA001_Daily_LogCreated_ViaEndpoint()
    {
        const string email = "us085_ca001_daily@awaken.app";
        await AuthenticateNewHunterAsync(email);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        var questId = Guid.NewGuid();
        var request = new CreateQuestLogRequest("daily", 100, null, null);

        var response = await _client.PostAsJsonAsync($"/api/quests/{questId}/logs", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogItemResponse>();
        result!.QuestType.Should().Be("daily");
        result.XpEarned.Should().Be(100);
        result.QuestId.Should().Be(questId);

        var log = await dbContext.QuestLogs.SingleOrDefaultAsync(l => l.QuestId == questId);
        log.Should().NotBeNull();
        log!.XpEarned.Should().Be(100);
        log.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task CA001_Dungeon_LogCreated_WithItems()
    {
        const string email = "us085_ca001_dungeon@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var questId = Guid.NewGuid();
        var request = new CreateQuestLogRequest("dungeon", 200, ["dungeon_stone"], null);

        var response = await _client.PostAsJsonAsync($"/api/quests/{questId}/logs", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogItemResponse>();
        result!.QuestType.Should().Be("dungeon");
        result.XpEarned.Should().Be(200);
        result.ItemsEarned.Should().Contain("dungeon_stone");
    }

    [Fact]
    public async Task CA001_Raid_LogCreated()
    {
        const string email = "us085_ca001_raid@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var questId = Guid.NewGuid();
        var request = new CreateQuestLogRequest("raid", 350, null, null);

        var response = await _client.PostAsJsonAsync($"/api/quests/{questId}/logs", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogItemResponse>();
        result!.QuestType.Should().Be("raid");
        result.XpEarned.Should().Be(350);
    }

    [Fact]
    public async Task CA001_WithXpPenalty_LogPreservesField()
    {
        const string email = "us085_ca001_penalty@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var questId = Guid.NewGuid();
        var request = new CreateQuestLogRequest("daily", 60, null, 40);

        var response = await _client.PostAsJsonAsync($"/api/quests/{questId}/logs", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogItemResponse>();
        result!.XpEarned.Should().Be(60);
        result.XpPenaltyApplied.Should().Be(40);
    }

    // ─── CA-002: sem duplicidade ────────────────────────────────────────────

    [Fact]
    public async Task CA002_DuplicateCall_ReturnsSameLog_NoNewEntry()
    {
        const string email = "us085_ca002@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var questId = Guid.NewGuid();
        var request = new CreateQuestLogRequest("daily", 80, null, null);

        var first = await _client.PostAsJsonAsync($"/api/quests/{questId}/logs", request);
        var second = await _client.PostAsJsonAsync($"/api/quests/{questId}/logs", request);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var r1 = await first.Content.ReadFromJsonAsync<BattleLogItemResponse>();
        var r2 = await second.Content.ReadFromJsonAsync<BattleLogItemResponse>();
        r1!.QuestLogId.Should().Be(r2!.QuestLogId, "deve retornar o mesmo log sem criar duplicata");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var count = await dbContext.QuestLogs.CountAsync(l => l.QuestId == questId);
        count.Should().Be(1, "idempotencia deve evitar duplicacao");
    }

    // ─── RN-007: tipo invalido nao gera log ─────────────────────────────────

    [Fact]
    public async Task RN007_InvalidQuestType_Returns400()
    {
        const string email = "us085_rn007@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var questId = Guid.NewGuid();
        var request = new CreateQuestLogRequest("cancelled", 0, null, null);

        var response = await _client.PostAsJsonAsync($"/api/quests/{questId}/logs", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var log = await dbContext.QuestLogs.SingleOrDefaultAsync(l => l.QuestId == questId);
        log.Should().BeNull("tipo invalido nao deve criar log");
    }

    // ─── RN-006: logs preservados após expiração ────────────────────────────

    [Fact]
    public async Task RN006_LogsPreserved_AfterTrialExpiry()
    {
        const string email = "us085_rn006@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var questId = Guid.NewGuid();
        (await _client.PostAsJsonAsync($"/api/quests/{questId}/logs",
            new CreateQuestLogRequest("daily", 90, null, null))).EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        typeof(Awaken.Domain.Entities.Auth.User)
            .GetProperty(nameof(Awaken.Domain.Entities.Auth.User.TrialEndsAt))!
            .SetValue(user, DateTime.UtcNow.AddDays(-1));
        await dbContext.SaveChangesAsync();

        var log = await dbContext.QuestLogs.SingleOrDefaultAsync(l => l.QuestId == questId);
        log.Should().NotBeNull("logs nao devem ser apagados apos o trial expirar");
    }

    // ─── Auth ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{Guid.NewGuid()}/logs",
            new CreateQuestLogRequest("daily", 100, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
