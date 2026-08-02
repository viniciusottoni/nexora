using System.Net;
using System.Text.Json;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

/// US-135: cobre o endpoint interno de aviso de quest diária perdida com penalidade contra Postgres real.
public class MissedDailyQuestNotificationEndpointTests : IAsyncLifetime
{
    private sealed class FixedDateTimeService(DateTime utcNow) : IDateTimeService
    {
        public DateTime UtcNow => utcNow;
        public DateOnly TodayUtc => DateOnly.FromDateTime(utcNow);
    }

    private sealed class RecordingPushNotificationService : IPushNotificationService
    {
        public sealed record PushCall(
            string PushToken,
            string Title,
            string Body,
            Dictionary<string, string>? Data);

        public List<PushCall> Calls { get; } = [];

        public Task SendAsync(
            string pushToken,
            string title,
            string body,
            Dictionary<string, string>? data = null,
            CancellationToken ct = default)
        {
            Calls.Add(new PushCall(
                pushToken,
                title,
                body,
                data is null ? null : new Dictionary<string, string>(data)));
            return Task.CompletedTask;
        }
    }

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("awaken")
        .WithUsername("awaken")
        .WithPassword("awaken_test_password")
        .Build();

    // Job roda após virada de dia; utcNow é o começo do novo dia.
    private readonly DateTime _utcNow = new(2026, 6, 28, 1, 0, 0, DateTimeKind.Utc);
    private readonly DateTime _yesterdayUtc = new(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc);

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private RecordingPushNotificationService _pushService = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _pushService = new RecordingPushNotificationService();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseProductionTestDefaults();
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PostgreSQL"] = _postgres.GetConnectionString(),
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDateTimeService>();
                services.AddScoped<IDateTimeService>(_ => new FixedDateTimeService(_utcNow));
                services.RemoveAll<IPushNotificationService>();
                services.AddSingleton<IPushNotificationService>(_pushService);
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

    private async Task<Guid> SeedUserWithMissedQuestAndPenaltyAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var user = User.Create($"missed_{Guid.NewGuid():N}@awaken.app", "hash", "Hunter");
        user.StartTrial(_utcNow.AddDays(7));
        dbContext.Users.Add(user);

        // Progressão com penalidade aplicada (TotalXp > 0 para que ApplyDailyMissPenalty gere valor > 0).
        var progression = HunterProgression.Create(user.Id);
        progression.AddXp(50, DateTime.UtcNow);
        progression.ApplyDailyMissPenalty(_yesterdayUtc);
        dbContext.HunterProgressions.Add(progression);

        // Quest diária de ontem perdida com penalidade verificada.
        var quest = Quest.Create(user.Id, _yesterdayUtc, "pt-BR", $"{user.Id:N}_daily");
        quest.MarkPenaltyChecked(_yesterdayUtc.AddHours(1));
        dbContext.Quests.Add(quest);

        var preference = NotificationPreference.Create(
            user.Id,
            pushEnabled: true,
            pushToken: "token-missed-quest",
            permissionStatus: "granted",
            _utcNow);
        dbContext.NotificationPreferences.Add(preference);

        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    private async Task<Guid> SeedUserWithCompletedQuestAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var user = User.Create($"completed_{Guid.NewGuid():N}@awaken.app", "hash", "Hunter");
        user.StartTrial(_utcNow.AddDays(7));
        dbContext.Users.Add(user);

        var progression = HunterProgression.Create(user.Id);
        dbContext.HunterProgressions.Add(progression);

        // Quest concluída — não deve gerar notificação (o repositório filtra status != completed).
        var quest = Quest.Create(user.Id, _yesterdayUtc, "pt-BR", $"{user.Id:N}_daily_done");
        quest.Complete(100, DateTime.UtcNow);
        quest.MarkPenaltyChecked(_yesterdayUtc.AddHours(1));
        dbContext.Quests.Add(quest);

        var preference = NotificationPreference.Create(
            user.Id,
            pushEnabled: true,
            pushToken: "token-completed-quest",
            permissionStatus: "granted",
            _utcNow);
        dbContext.NotificationPreferences.Add(preference);

        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    private async Task<Guid> SeedUserWithExpiredAccessAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var user = User.Create($"expired_{Guid.NewGuid():N}@awaken.app", "hash", "Hunter");
        user.StartTrial(_utcNow.AddDays(-1)); // trial expirado
        dbContext.Users.Add(user);

        var progression = HunterProgression.Create(user.Id);
        progression.AddXp(50, DateTime.UtcNow);
        progression.ApplyDailyMissPenalty(_yesterdayUtc);
        dbContext.HunterProgressions.Add(progression);

        var quest = Quest.Create(user.Id, _yesterdayUtc, "pt-BR", $"{user.Id:N}_daily_expired");
        quest.MarkPenaltyChecked(_yesterdayUtc.AddHours(1));
        dbContext.Quests.Add(quest);

        var preference = NotificationPreference.Create(
            user.Id,
            pushEnabled: true,
            pushToken: "token-expired-access",
            permissionStatus: "granted",
            _utcNow);
        dbContext.NotificationPreferences.Add(preference);

        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    /// CA-001: quest perdida com penalidade e acesso ativo → envia push.
    [Fact]
    public async Task CA001_RunMissedDailyQuest_SendsToEligibleUser()
    {
        var userId = await SeedUserWithMissedQuestAndPenaltyAsync();

        var response = await _client.PostAsync("/internal/notifications/missed-daily-quest/run", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("sent").GetInt32().Should().BeGreaterThanOrEqualTo(1);

        _pushService.Calls.Should().Contain(c =>
            c.PushToken == "token-missed-quest" &&
            c.Data!.ContainsKey("type") && c.Data["type"] == "missed_daily_quest_notification" &&
            c.Data.ContainsKey("route") && c.Data["route"] == "/daily-quest");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var preference = await dbContext.NotificationPreferences.SingleAsync(np => np.UserId == userId);
        preference.DailyNotificationCount.Should().Be(1);
        preference.LastNotificationSentAt.Should().Be(_utcNow);
    }

    /// CA-002: quest concluída → não recebe notificação de quest perdida.
    [Fact]
    public async Task CA002_RunMissedDailyQuest_SkipsCompletedQuestUser()
    {
        await SeedUserWithCompletedQuestAsync();

        var response = await _client.PostAsync("/internal/notifications/missed-daily-quest/run", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _pushService.Calls.Should().NotContain(c => c.PushToken == "token-completed-quest");
    }

    /// RN-002: acesso expirado → não recebe notificação.
    [Fact]
    public async Task RN002_RunMissedDailyQuest_SkipsExpiredAccessUser()
    {
        await SeedUserWithExpiredAccessAsync();

        var response = await _client.PostAsync("/internal/notifications/missed-daily-quest/run", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _pushService.Calls.Should().NotContain(c => c.PushToken == "token-expired-access");
    }

    /// Resposta inclui campos eligible, sent e skipped.
    [Fact]
    public async Task Response_ContainsExpectedFields()
    {
        var response = await _client.PostAsync("/internal/notifications/missed-daily-quest/run", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.TryGetProperty("eligible", out _).Should().BeTrue();
        document.RootElement.TryGetProperty("sent", out _).Should().BeTrue();
        document.RootElement.TryGetProperty("skipped", out _).Should().BeTrue();
    }
}
