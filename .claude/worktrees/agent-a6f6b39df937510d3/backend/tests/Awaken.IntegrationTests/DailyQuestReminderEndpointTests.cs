using System.Net;
using System.Text.Json;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Entities.Quests;
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

public class DailyQuestReminderEndpointTests : IAsyncLifetime
{
    private sealed class AllowAllNotificationEligibilityService : INotificationEligibilityService
    {
        public Task<EligibilityResult> EvaluateAsync(
            Guid userId,
            string notificationType,
            DateTime utcNow,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(EligibilityResult.Allow());
    }

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

    private readonly DateTime _utcNow = new(2026, 6, 27, 9, 0, 0, DateTimeKind.Utc);
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

                services.AddScoped<INotificationEligibilityService, AllowAllNotificationEligibilityService>();
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

    private WebApplicationFactory<Program> CreateFactory(DateTime utcNow, RecordingPushNotificationService pushService) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
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
                services.AddScoped<IDateTimeService>(_ => new FixedDateTimeService(utcNow));

                services.AddScoped<INotificationEligibilityService, AllowAllNotificationEligibilityService>();
                services.RemoveAll<IPushNotificationService>();
                services.AddSingleton<IPushNotificationService>(pushService);
            });
        });

    private static async Task MigrateFactoryAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    private async Task<Guid> SeedEligibleUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var user = User.Create("daily_reminder@awaken.app", "hash", "Hunter");
        user.StartTrial(_utcNow.AddDays(7));
        dbContext.Users.Add(user);

        var preference = NotificationPreference.Create(
            user.Id,
            pushEnabled: true,
            pushToken: "token-daily-reminder",
            permissionStatus: "granted",
            _utcNow);
        dbContext.NotificationPreferences.Add(preference);

        var questDateUtc = DateTime.SpecifyKind(_utcNow.Date, DateTimeKind.Utc);
        var quest = Quest.Create(user.Id, questDateUtc, "pt-BR", $"{user.Id:N}_{questDateUtc:yyyyMMdd}_daily");
        dbContext.Quests.Add(quest);

        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    private static async Task<Guid> SeedEligibleUserAsync(
        IServiceProvider services,
        DateTime utcNow,
        string timezone,
        TimeOnly preferredReminderTime)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var email = $"daily_reminder_{Guid.NewGuid():N}@awaken.app";
        var user = User.Create(email, "hash", "Hunter");
        user.StartTrial(utcNow.AddDays(7));
        dbContext.Users.Add(user);

        var preference = NotificationPreference.Create(
            user.Id,
            pushEnabled: true,
            pushToken: "token-daily-reminder",
            permissionStatus: "granted",
            utcNow);
        preference.UpdateReminderTime(preferredReminderTime, timezone, utcNow);
        dbContext.NotificationPreferences.Add(preference);

        var questDateUtc = DateTime.SpecifyKind(utcNow.Date, DateTimeKind.Utc);
        var quest = Quest.Create(user.Id, questDateUtc, "pt-BR", $"{user.Id:N}_{questDateUtc:yyyyMMdd}_daily");
        dbContext.Quests.Add(quest);

        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task CA001_RunDailyQuestReminders_ReturnsOk_AndProcessesEligibleUser()
    {
        var userId = await SeedEligibleUserAsync();

        var response = await _client.PostAsync("/internal/notifications/daily-quest-reminders/run", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("eligible").GetInt32().Should().Be(1);
        document.RootElement.GetProperty("sent").GetInt32().Should().Be(1);
        document.RootElement.GetProperty("skipped").GetInt32().Should().Be(0);

        _pushService.Calls.Should().ContainSingle();
        var call = _pushService.Calls[0];
        call.PushToken.Should().Be("token-daily-reminder");
        call.Data.Should().NotBeNull();
        call.Data!.Should().ContainKey("type").WhoseValue.Should().Be("daily_quest_reminder");
        call.Data.Should().ContainKey("route").WhoseValue.Should().Be("/daily-quest");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var preference = await dbContext.NotificationPreferences.SingleAsync(np => np.UserId == userId);

        preference.DailyNotificationCount.Should().Be(1);
        preference.LastNotificationCountResetDate.Should().Be(DateOnly.FromDateTime(_utcNow));
        preference.LastNotificationSentAt.Should().Be(_utcNow);
    }

    [Fact]
    public async Task CA002_RunDailyQuestReminders_SkipsBeforePreferredLocalTime()
    {
        var utcNow = new DateTime(2026, 6, 27, 11, 30, 0, DateTimeKind.Utc);
        var pushService = new RecordingPushNotificationService();
        await using var factory = CreateFactory(utcNow, pushService);
        await MigrateFactoryAsync(factory);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

        await SeedEligibleUserAsync(factory.Services, utcNow, "America/Sao_Paulo", new TimeOnly(9, 30));

        var response = await client.PostAsync("/internal/notifications/daily-quest-reminders/run", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("eligible").GetInt32().Should().Be(1);
        document.RootElement.GetProperty("sent").GetInt32().Should().Be(0);
        document.RootElement.GetProperty("skipped").GetInt32().Should().Be(1);

        pushService.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task CA003_RunDailyQuestReminders_SendsAtPreferredLocalTime()
    {
        var utcNow = new DateTime(2026, 6, 27, 12, 30, 0, DateTimeKind.Utc);
        var pushService = new RecordingPushNotificationService();
        await using var factory = CreateFactory(utcNow, pushService);
        await MigrateFactoryAsync(factory);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

        await SeedEligibleUserAsync(factory.Services, utcNow, "America/Sao_Paulo", new TimeOnly(9, 30));

        var response = await client.PostAsync("/internal/notifications/daily-quest-reminders/run", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("eligible").GetInt32().Should().Be(1);
        document.RootElement.GetProperty("sent").GetInt32().Should().Be(1);
        document.RootElement.GetProperty("skipped").GetInt32().Should().Be(0);

        pushService.Calls.Should().ContainSingle();
    }
}
