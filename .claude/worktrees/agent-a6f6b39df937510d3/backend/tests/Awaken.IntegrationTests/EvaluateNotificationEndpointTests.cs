using System.Net;
using System.Net.Http.Json;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Notifications;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

/// US-095: cobre POST /internal/notifications/evaluate contra Postgres real.
public class EvaluateNotificationEndpointTests : IAsyncLifetime
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

    private static async Task<Guid> SeedEligibleUserAsync(AwakenDbContext db)
    {
        var utcNow = DateTime.UtcNow;
        var user = User.Create($"{Guid.NewGuid():N}@awaken.app", "hash", "Hunter", "pt-BR");
        user.StartTrial(utcNow.AddDays(7));
        db.Users.Add(user);

        var pref = NotificationPreference.Create(user.Id, true, "fcm-eval-token", "granted", utcNow);
        db.NotificationPreferences.Add(pref);

        await db.SaveChangesAsync();
        return user.Id;
    }

    private static async Task<Guid> SeedUserWithLimitReachedAsync(AwakenDbContext db)
    {
        var utcNow = DateTime.UtcNow;
        var user = User.Create($"{Guid.NewGuid():N}@awaken.app", "hash", "Hunter", "pt-BR");
        user.StartTrial(utcNow.AddDays(7));
        db.Users.Add(user);

        var pref = NotificationPreference.Create(user.Id, true, "fcm-limit-token", "granted", utcNow);
        pref.RecordNotificationSent(utcNow);
        pref.RecordNotificationSent(utcNow);
        pref.RecordNotificationSent(utcNow);
        db.NotificationPreferences.Add(pref);

        await db.SaveChangesAsync();
        return user.Id;
    }

    // CA-001: usuário elegível → allowed=true e log persistido com status "sent"
    [Fact]
    public async Task CA001_EligibleUser_ReturnsAllowedAndPersistsLog()
    {
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            userId = await SeedEligibleUserAsync(db);
        }

        var response = await _client.PostAsJsonAsync("/internal/notifications/evaluate", new
        {
            userId,
            notificationType = "daily_quest_reminder"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
        body!.Allowed.Should().BeTrue();
        body.BlockReason.Should().BeNull();
        body.LogId.Should().NotBeEmpty();

        using var assertScope = _factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var log = await assertDb.NotificationLogs.SingleAsync(l => l.UserId == userId);
        log.DecisionStatus.Should().Be("sent");
        log.DecisionReason.Should().BeNull();
        log.NotificationType.Should().Be("daily_quest_reminder");
    }

    // CA-001: limite diário atingido → blocked com reason "daily_limit_reached" e log "ignored"
    [Fact]
    public async Task CA001_DailyLimitReached_ReturnsBlockedAndPersistsLog()
    {
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            userId = await SeedUserWithLimitReachedAsync(db);
        }

        var response = await _client.PostAsJsonAsync("/internal/notifications/evaluate", new
        {
            userId,
            notificationType = "daily_quest_reminder"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
        body!.Allowed.Should().BeFalse();
        body.BlockReason.Should().Be("daily_limit_reached");

        using var assertScope = _factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var log = await assertDb.NotificationLogs.SingleAsync(l => l.UserId == userId);
        log.DecisionStatus.Should().Be("ignored");
        log.DecisionReason.Should().Be("daily_limit_reached");
    }

    // CA-002: limite atingido + streak_risk_alert (HIGH priority) → ainda permitido
    [Fact]
    public async Task CA002_HighPriorityBypassesDailyLimit()
    {
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            userId = await SeedUserWithLimitReachedAsync(db);
        }

        var response = await _client.PostAsJsonAsync("/internal/notifications/evaluate", new
        {
            userId,
            notificationType = "streak_risk_alert"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
        body!.Allowed.Should().BeTrue();

        using var assertScope = _factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var log = await assertDb.NotificationLogs.SingleAsync(l => l.UserId == userId);
        log.DecisionStatus.Should().Be("sent");
    }

    // US-095: se streak_risk_alert já foi enviada hoje, lembrete comum deve ser bloqueado.
    [Fact]
    public async Task CA003_HigherPriorityAlreadySent_BlocksReminder()
    {
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            userId = await SeedEligibleUserAsync(db);
            var attemptedAtUtc = DateTime.UtcNow;
            db.NotificationLogs.Add(NotificationLog.Create(
                userId,
                "streak_risk_alert",
                "sent",
                null,
                attemptedAtUtc));
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync("/internal/notifications/evaluate", new
        {
            userId,
            notificationType = "daily_quest_reminder"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
        body!.Allowed.Should().BeFalse();
        body.BlockReason.Should().Be("higher_priority_already_sent");

        using var assertScope = _factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var log = await assertDb.NotificationLogs.OrderBy(l => l.CreatedAtUtc).ToListAsync();
        log.Should().HaveCount(2);
        log.Last().DecisionStatus.Should().Be("ignored");
        log.Last().DecisionReason.Should().Be("higher_priority_already_sent");
    }

    // RN-005: usuário sem preferência → blocked "no_consent" e log persistido
    [Fact]
    public async Task RN005_NoPreference_ReturnsBlockedNoConsentAndPersistsLog()
    {
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var user = User.Create($"{Guid.NewGuid():N}@awaken.app", "hash", "Hunter");
            user.StartTrial(DateTime.UtcNow.AddDays(7));
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;
        }

        var response = await _client.PostAsJsonAsync("/internal/notifications/evaluate", new
        {
            userId,
            notificationType = "daily_quest_reminder"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
        body!.Allowed.Should().BeFalse();
        body.BlockReason.Should().Be("no_consent");

        using var assertScope = _factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var log = await assertDb.NotificationLogs.SingleAsync(l => l.UserId == userId);
        log.DecisionStatus.Should().Be("ignored");
        log.DecisionReason.Should().Be("no_consent");
    }

    private sealed record EvaluateResponse(bool Allowed, string? BlockReason, Guid LogId);
}
