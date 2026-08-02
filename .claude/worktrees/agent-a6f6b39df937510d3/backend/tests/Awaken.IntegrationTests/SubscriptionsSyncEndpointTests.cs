using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Subscriptions;
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

public class SubscriptionsSyncEndpointTests : IAsyncLifetime
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

    private async Task<string> RegisterAndGetTokenAsync(string email = "hunter@awaken.app")
    {
        var payload = new { email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.AccessToken;
    }

    // US-194: subscription state must be seeded via DB (webhook-driven) — sync only correlates customer ID.
    private async Task SeedPaidSubscriptionAsync(
        string email, string plan, DateTime expiresAt, string revenueCatCustomerId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var user = await db.Users.FirstAsync(u => u.Email == email);
        var now = DateTime.UtcNow;

        var existing = await db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (existing is not null)
        {
            db.Subscriptions.Remove(existing);
            await db.SaveChangesAsync();
        }

        var subscription = Subscription.CreateFromPaidPlan(
            user.Id, plan, "pro_access", revenueCatCustomerId, expiresAt, now);
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task SyncReturnsSubscriptionActiveForFutureExpiry()
    {
        var token = await RegisterAndGetTokenAsync("sync_active@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(30);
        await SeedPaidSubscriptionAsync("sync_active@awaken.app", "monthly", expiresAt, "rc_customer_001");

        var payload = new SyncEntitlementRequest("rc_customer_001");
        var response = await _client.PostAsJsonAsync("/api/subscriptions/sync", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SyncEntitlementResponse>();
        body!.AccessStatus.Should().Be("subscription_active");
        body.Plan.Should().Be("monthly");
        body.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(2));
        body.AccessRestored.Should().BeFalse();
    }

    [Fact]
    public async Task SyncReturnsSubscriptionExpiredForPastExpiry()
    {
        var token = await RegisterAndGetTokenAsync("sync_expired@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(-5);
        await SeedPaidSubscriptionAsync("sync_expired@awaken.app", "annual", expiresAt, "rc_customer_002");

        var payload = new SyncEntitlementRequest("rc_customer_002");
        var response = await _client.PostAsJsonAsync("/api/subscriptions/sync", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SyncEntitlementResponse>();
        body!.AccessStatus.Should().Be("subscription_expired");
        body.AccessRestored.Should().BeFalse();
    }

    [Fact]
    public async Task SyncIsIdempotentWhenCalledMultipleTimes()
    {
        var token = await RegisterAndGetTokenAsync("sync_idempotent@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(30);
        await SeedPaidSubscriptionAsync("sync_idempotent@awaken.app", "monthly", expiresAt, "rc_customer_003");

        var payload = new SyncEntitlementRequest("rc_customer_003");
        var first = await _client.PostAsJsonAsync("/api/subscriptions/sync", payload);
        var second = await _client.PostAsJsonAsync("/api/subscriptions/sync", payload);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await second.Content.ReadFromJsonAsync<SyncEntitlementResponse>();
        body!.AccessStatus.Should().Be("subscription_active");
        body.AccessRestored.Should().BeFalse();
    }

    [Fact]
    public async Task SyncAfterTrialSetsSubscriptionActiveAndStatusReflectsIt()
    {
        var token = await RegisterAndGetTokenAsync("sync_after_trial@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsync("/api/subscriptions/trial/start", null);

        // US-194: paid plan is established via webhook/DB seeding, not via sync payload.
        var expiresAt = DateTime.UtcNow.AddDays(30);
        await SeedPaidSubscriptionAsync("sync_after_trial@awaken.app", "annual", expiresAt, "rc_customer_004");

        var syncPayload = new SyncEntitlementRequest("rc_customer_004");
        var syncResponse = await _client.PostAsJsonAsync("/api/subscriptions/sync", syncPayload);
        syncResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var statusResponse = await _client.GetAsync("/api/subscriptions/status");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await statusResponse.Content.ReadFromJsonAsync<SubscriptionStatusResponse>();
        status!.AccessStatus.Should().Be("subscription_active");
        status.Plan.Should().Be("annual");
    }

    [Fact]
    public async Task SyncWithEmptyCustomerIdReturnsBadRequest()
    {
        var token = await RegisterAndGetTokenAsync("sync_empty_id@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new SyncEntitlementRequest("");
        var response = await _client.PostAsJsonAsync("/api/subscriptions/sync", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task SyncCreatesAnnualSubscriptionActiveForNewUser()
    {
        var token = await RegisterAndGetTokenAsync("sync_annual_active@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(365);
        await SeedPaidSubscriptionAsync("sync_annual_active@awaken.app", "annual", expiresAt, "rc_customer_007");

        var payload = new SyncEntitlementRequest("rc_customer_007");
        var response = await _client.PostAsJsonAsync("/api/subscriptions/sync", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SyncEntitlementResponse>();
        body!.AccessStatus.Should().Be("subscription_active");
        body.Plan.Should().Be("annual");
        body.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(2));
        body.AccessRestored.Should().BeFalse();
    }

    [Fact]
    public async Task SyncLinksRevenueCatCustomerIdToExistingSubscription()
    {
        var token = await RegisterAndGetTokenAsync("sync_link_id@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Seed subscription without a RevenueCat customer ID yet (empty string = not linked).
        var expiresAt = DateTime.UtcNow.AddDays(30);
        await SeedPaidSubscriptionAsync("sync_link_id@awaken.app", "monthly", expiresAt, "");

        // Sync with a new customer ID — should link it without changing plan/status.
        var payload = new SyncEntitlementRequest("rc_link_customer_008");
        var response = await _client.PostAsJsonAsync("/api/subscriptions/sync", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SyncEntitlementResponse>();
        body!.AccessStatus.Should().Be("subscription_active");

        // Verify the customer ID was persisted in the DB.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == "sync_link_id@awaken.app");
        var sub = await db.Subscriptions.FirstAsync(s => s.UserId == user.Id);
        sub.RevenueCatCustomerId.Should().Be("rc_link_customer_008");
    }

    [Fact]
    public async Task SyncReturnsNoSubscriptionForNewUserWithoutWebhook()
    {
        var token = await RegisterAndGetTokenAsync("sync_no_sub@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // A brand new user with no subscription — sync should return no_subscription.
        var payload = new SyncEntitlementRequest("rc_customer_new_user");
        var response = await _client.PostAsJsonAsync("/api/subscriptions/sync", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SyncEntitlementResponse>();
        body!.AccessStatus.Should().Be("no_subscription");
    }

    [Fact]
    public async Task SyncReturnsUnauthorizedWhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var payload = new SyncEntitlementRequest("rc_customer_006");
        var response = await _client.PostAsJsonAsync("/api/subscriptions/sync", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
