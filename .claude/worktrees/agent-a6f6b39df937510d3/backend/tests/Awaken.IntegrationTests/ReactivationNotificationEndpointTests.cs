using System.Net;
using System.Text.Json;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Notifications;
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

/// US-124: cobre o endpoint interno de comunicacao de reativacao contra Postgres real.
public class ReactivationNotificationEndpointTests : IAsyncLifetime
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

    private readonly DateTime _utcNow = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);
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

    private async Task<Guid> SeedExpiredTrialUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var user = User.Create($"reactivation_{Guid.NewGuid():N}@awaken.app", "hash", "Hunter");
        dbContext.Users.Add(user);

        var subscription = Subscription.CreateTrial(user.Id, _utcNow.AddDays(-8), _utcNow.AddDays(-1));
        subscription.ExpireTrial();
        dbContext.Subscriptions.Add(subscription);

        var preference = NotificationPreference.Create(
            user.Id,
            pushEnabled: true,
            pushToken: "token-reactivation",
            permissionStatus: "granted",
            _utcNow);
        dbContext.NotificationPreferences.Add(preference);

        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    private async Task<Guid> SeedActiveSubscriberAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var user = User.Create($"subscriber_{Guid.NewGuid():N}@awaken.app", "hash", "Hunter");
        dbContext.Users.Add(user);

        var subscription = Subscription.CreateFromPaidPlan(
            user.Id, "monthly", "premium", "rc-sub", _utcNow.AddDays(30), _utcNow);
        dbContext.Subscriptions.Add(subscription);

        var preference = NotificationPreference.Create(
            user.Id,
            pushEnabled: true,
            pushToken: "token-active-sub",
            permissionStatus: "granted",
            _utcNow);
        dbContext.NotificationPreferences.Add(preference);

        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    /// CA-001: usuario com trial expirado recebe push de reativacao.
    [Fact]
    public async Task CA001_RunReactivation_SendsToExpiredTrialUser()
    {
        var userId = await SeedExpiredTrialUserAsync();

        var response = await _client.PostAsync("/internal/notifications/reactivation/run", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("sent").GetInt32().Should().BeGreaterThanOrEqualTo(1);

        _pushService.Calls.Should().Contain(c =>
            c.PushToken == "token-reactivation" &&
            c.Data!.ContainsKey("type") && c.Data["type"] == "reactivation_notification" &&
            c.Data.ContainsKey("route") && c.Data["route"] == "/subscription");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var preference = await dbContext.NotificationPreferences.SingleAsync(np => np.UserId == userId);
        preference.LastReactivationNotificationSentAt.Should().Be(_utcNow);
    }

    /// CA-002: assinante ativo nao recebe push de reativacao.
    [Fact]
    public async Task CA002_RunReactivation_SkipsActiveSubscriber()
    {
        await SeedActiveSubscriberAsync();

        var response = await _client.PostAsync("/internal/notifications/reactivation/run", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _pushService.Calls.Should().NotContain(c => c.PushToken == "token-active-sub");
    }

    /// RN-004: usuario que ja recebeu reativacao recentemente nao recebe novamente.
    [Fact]
    public async Task RN004_FrequencyNotMet_DoesNotResend()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var user = User.Create($"reactivation_freq_{Guid.NewGuid():N}@awaken.app", "hash", "Hunter");
        dbContext.Users.Add(user);

        var subscription = Subscription.CreateTrial(user.Id, _utcNow.AddDays(-8), _utcNow.AddDays(-1));
        subscription.ExpireTrial();
        dbContext.Subscriptions.Add(subscription);

        var preference = NotificationPreference.Create(
            user.Id,
            pushEnabled: true,
            pushToken: "token-reactivation-freq",
            permissionStatus: "granted",
            _utcNow);
        // Simula envio recente (3 dias atras — menos que 7 dias minimo).
        preference.RecordReactivationNotificationSent(_utcNow.AddDays(-3));
        dbContext.NotificationPreferences.Add(preference);

        await dbContext.SaveChangesAsync();

        var response = await _client.PostAsync("/internal/notifications/reactivation/run", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _pushService.Calls.Should().NotContain(c => c.PushToken == "token-reactivation-freq");
    }
}
