using System.Net;
using System.Net.Http.Json;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Notifications.Commands.SendStreakRiskAlert;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Entities.Progression;
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

/// US-093: cobre o endpoint interno do alerta de streak em risco contra Postgres real.
public class StreakRiskAlertEndpointTests : IAsyncLifetime
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

    private readonly DateTime _utcNow = new(2026, 6, 27, 18, 0, 0, DateTimeKind.Utc);
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private RecordingPushNotificationService _pushService = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _pushService = new RecordingPushNotificationService();
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

    private static async Task<Guid> SeedEligibleUserAsync(AwakenDbContext dbContext)
    {
        var utcNow = new DateTime(2026, 6, 27, 18, 0, 0, DateTimeKind.Utc);

        var user = User.Create(
            $"{Guid.NewGuid():N}@awaken.app",
            "hash",
            "Hunter",
            "pt-BR");
        user.StartTrial(utcNow.AddDays(7));
        dbContext.Users.Add(user);

        var progression = HunterProgression.Create(user.Id);
        typeof(HunterProgression)
            .GetProperty(nameof(HunterProgression.CurrentStreakDays))!
            .SetValue(progression, 4);
        dbContext.HunterProgressions.Add(progression);

        var quest = Quest.Create(
            user.Id,
            utcNow.Date,
            "pt-BR",
            $"{user.Id:N}_{utcNow:yyyyMMdd}_daily");
        dbContext.Quests.Add(quest);

        var preference = NotificationPreference.Create(
            user.Id,
            pushEnabled: true,
            pushToken: "fcm-streak-risk-token",
            permissionStatus: "granted",
            utcNow);
        dbContext.NotificationPreferences.Add(preference);

        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task CA001_InternalRunEndpoint_ReturnsOk_AndProcessesEligibleUser()
    {
        Guid userId;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            userId = await SeedEligibleUserAsync(dbContext);
        }

        var response = await _client.PostAsync("/internal/notifications/streak-risk/run", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SendStreakRiskAlertResult>();
        body!.Eligible.Should().Be(1);
        body.Sent.Should().Be(1);
        body.Skipped.Should().Be(0);

        _pushService.Calls.Should().ContainSingle();
        var call = _pushService.Calls[0];
        call.PushToken.Should().Be("fcm-streak-risk-token");
        call.Data.Should().NotBeNull();
        call.Data!.Should().ContainKey("type").WhoseValue.Should().Be("streak_risk_alert");
        call.Data.Should().ContainKey("route").WhoseValue.Should().Be("/daily-quest");

        using var assertScope = _factory.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var preference = await assertContext.NotificationPreferences.SingleAsync(np => np.UserId == userId);

        preference.DailyNotificationCount.Should().Be(1);
        preference.LastNotificationSentAt.Should().NotBeNull();
        preference.LastNotificationCountResetDate.Should().Be(DateOnly.FromDateTime(_utcNow));
    }
}
