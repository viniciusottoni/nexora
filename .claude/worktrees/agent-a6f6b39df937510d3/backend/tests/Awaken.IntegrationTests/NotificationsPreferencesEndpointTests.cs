using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Notifications;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class NotificationsPreferencesEndpointTests : IAsyncLifetime
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
            builder.ConfigureServices(services =>
            {
                services.AddScoped<INotificationEligibilityService, AllowAllNotificationEligibilityService>();
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

    private async Task<string> RegisterAndGetTokenAsync(string email = "hunter@awaken.app")
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Str0ngPass!",
            name = "Hunter",
            language = "pt-BR"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.AccessToken;
    }

    private async Task SeedPaidSubscriptionAsync(
        string email, string plan, DateTime expiresAt, string revenueCatCustomerId = "rc_test_customer")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        var now = DateTime.UtcNow;

        var existing = await db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (existing is not null)
        {
            db.Subscriptions.Remove(existing);
            await db.SaveChangesAsync();
        }

        db.Subscriptions.Add(
            Subscription.CreateFromPaidPlan(user.Id, plan, "pro_access", revenueCatCustomerId, expiresAt, now));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task PutPreferencesReturns401WhenUnauthenticated()
    {
        var response = await _client.PutAsJsonAsync("/api/notifications/preferences", new
        {
            pushEnabled = true,
            pushToken = "fcm-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPreferencesReturns401WhenUnauthenticated()
    {
        var response = await _client.GetAsync("/api/notifications/preferences");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPreferencesReturns404WhenNoPreferenceSaved()
    {
        var token = await RegisterAndGetTokenAsync("noprefs@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/notifications/preferences");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutPreferencesReturnsOkWithValidData()
    {
        var token = await RegisterAndGetTokenAsync("putprefs@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync("/api/notifications/preferences", new
        {
            pushEnabled = true,
            pushToken = "fcm-token-abc123",
            permissionStatus = "granted"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<NotificationPreferencesResponse>();
        body!.PushEnabled.Should().BeTrue();
        body.PushToken.Should().Be("fcm-token-abc123");
        body.PermissionStatus.Should().Be("granted");
    }

    [Fact]
    public async Task ExpiredAccessUserCanStillUpdateNotificationPreferences()
    {
        var token = await RegisterAndGetTokenAsync("expirednotif@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await SeedPaidSubscriptionAsync(
            "expirednotif@awaken.app",
            "monthly",
            DateTime.UtcNow.AddDays(-1),
            "rc-expired-notif");

        var response = await _client.PutAsJsonAsync("/api/notifications/preferences", new
        {
            pushEnabled = true,
            pushToken = "fcm-token-expired-access",
            permissionStatus = "token_registered"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<NotificationPreferencesResponse>();
        body!.PushEnabled.Should().BeTrue();
        body.PushToken.Should().Be("fcm-token-expired-access");
        body.PermissionStatus.Should().Be("token_registered");
    }

    [Fact]
    public async Task PutPreferencesAllowsGrantedWithoutToken()
    {
        var token = await RegisterAndGetTokenAsync("grantednotoken@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync("/api/notifications/preferences", new
        {
            pushEnabled = true,
            pushToken = (string?)null,
            permissionStatus = "granted"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<NotificationPreferencesResponse>();
        body!.PushEnabled.Should().BeTrue();
        body.PushToken.Should().BeNull();
        body.PermissionStatus.Should().Be("granted");
    }

    [Fact]
    public async Task PutPreferencesStillWorksForExpiredAccessUser()
    {
        var token = await RegisterAndGetTokenAsync("expiredprefs@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await SeedPaidSubscriptionAsync("expiredprefs@awaken.app", "monthly", DateTime.UtcNow.AddDays(-1), "rc_expired_notifications");

        var response = await _client.PutAsJsonAsync("/api/notifications/preferences", new
        {
            pushEnabled = true,
            pushToken = (string?)null,
            permissionStatus = "granted"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<NotificationPreferencesResponse>();
        body!.PushEnabled.Should().BeTrue();
        body.PushToken.Should().BeNull();
        body.PermissionStatus.Should().Be("granted");
    }

    [Fact]
    public async Task GetPreferencesReturnsSavedPreferenceAfterPut()
    {
        var token = await RegisterAndGetTokenAsync("getprefs@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PutAsJsonAsync("/api/notifications/preferences", new
        {
            pushEnabled = true,
            pushToken = "fcm-token-xyz789",
            permissionStatus = "token_registered"
        });

        var response = await _client.GetAsync("/api/notifications/preferences");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<NotificationPreferencesResponse>();
        body!.PushEnabled.Should().BeTrue();
        body.PushToken.Should().Be("fcm-token-xyz789");
        body.PermissionStatus.Should().Be("token_registered");
    }

    [Fact]
    public async Task FullCyclePutEnableThenDisableThenVerify()
    {
        var token = await RegisterAndGetTokenAsync("cycle@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Step 1: Enable push with token
        var enableResponse = await _client.PutAsJsonAsync("/api/notifications/preferences", new
        {
            pushEnabled = true,
            pushToken = "fcm-token-cycle",
            permissionStatus = "granted"
        });
        enableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Confirm via GET
        var getAfterEnable = await _client.GetAsync("/api/notifications/preferences");
        getAfterEnable.StatusCode.Should().Be(HttpStatusCode.OK);
        var enabledBody = await getAfterEnable.Content.ReadFromJsonAsync<NotificationPreferencesResponse>();
        enabledBody!.PushEnabled.Should().BeTrue();
        enabledBody.PushToken.Should().Be("fcm-token-cycle");

        // Step 3: Disable push — token should be cleared
        var disableResponse = await _client.PutAsJsonAsync("/api/notifications/preferences", new
        {
            pushEnabled = false,
            pushToken = (string?)null,
            permissionStatus = "denied"
        });
        disableResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var disabledBody = await disableResponse.Content.ReadFromJsonAsync<NotificationPreferencesResponse>();
        disabledBody!.PushEnabled.Should().BeFalse();
        disabledBody.PushToken.Should().BeNull();

        // Step 4: Confirm via GET
        var getAfterDisable = await _client.GetAsync("/api/notifications/preferences");
        getAfterDisable.StatusCode.Should().Be(HttpStatusCode.OK);
        var finalBody = await getAfterDisable.Content.ReadFromJsonAsync<NotificationPreferencesResponse>();
        finalBody!.PushEnabled.Should().BeFalse();
        finalBody.PushToken.Should().BeNull();
        finalBody.PermissionStatus.Should().Be("denied");
    }

    [Fact]
    public async Task PutPreferencesReturns422WithInvalidPermissionStatus()
    {
        var token = await RegisterAndGetTokenAsync("badstatus@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync("/api/notifications/preferences", new
        {
            pushEnabled = false,
            pushToken = (string?)null,
            permissionStatus = "INVALID_STATUS"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PatchReminderTimeReturns401WhenUnauthenticated()
    {
        var response = await _client.PatchAsJsonAsync("/api/notifications/preferences/reminder-time", new
        {
            preferredReminderTime = "19:30",
            timezone = "America/Recife"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PatchReminderTimeWithValidDataReturnsOk()
    {
        var token = await RegisterAndGetTokenAsync("patchtime@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/notifications/preferences/reminder-time", new
        {
            preferredReminderTime = "19:30",
            timezone = "America/Recife"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<NotificationPreferencesResponse>();
        body!.PreferredReminderTime.Should().Be(new TimeOnly(19, 30));
        body.Timezone.Should().Be("America/Recife");
    }

    [Fact]
    public async Task PatchReminderTimePersistsAndVisibleOnGet()
    {
        var token = await RegisterAndGetTokenAsync("patchget@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PatchAsJsonAsync("/api/notifications/preferences/reminder-time", new
        {
            preferredReminderTime = "08:00",
            timezone = "America/Sao_Paulo"
        });

        var getResponse = await _client.GetAsync("/api/notifications/preferences");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await getResponse.Content.ReadFromJsonAsync<NotificationPreferencesResponse>();
        body!.PreferredReminderTime.Should().Be(new TimeOnly(8, 0));
        body.Timezone.Should().Be("America/Sao_Paulo");
    }

    [Fact]
    public async Task PatchReminderTimeWithInvalidTimeFormatReturns422()
    {
        var token = await RegisterAndGetTokenAsync("badtime@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/notifications/preferences/reminder-time", new
        {
            preferredReminderTime = "7:30",
            timezone = "America/Recife"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("INVALID_TIME_FORMAT");
        document.RootElement.GetProperty("message").GetString().Should().Be("Time must be in HH:mm format.");
        document.RootElement.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PatchReminderTimeWithEmptyTimezoneReturns422()
    {
        var token = await RegisterAndGetTokenAsync("emptytimezone@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/notifications/preferences/reminder-time", new
        {
            preferredReminderTime = "19:30",
            timezone = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ExpiredAccessUserCanStillUpdateReminderTime()
    {
        var token = await RegisterAndGetTokenAsync("expiredtime@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await SeedPaidSubscriptionAsync(
            "expiredtime@awaken.app",
            "monthly",
            DateTime.UtcNow.AddDays(-1),
            "rc-expired-time");

        var response = await _client.PatchAsJsonAsync("/api/notifications/preferences/reminder-time", new
        {
            preferredReminderTime = "20:00",
            timezone = "America/Recife"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<NotificationPreferencesResponse>();
        body!.PreferredReminderTime.Should().Be(new TimeOnly(20, 0));
        body.Timezone.Should().Be("America/Recife");
    }
}
