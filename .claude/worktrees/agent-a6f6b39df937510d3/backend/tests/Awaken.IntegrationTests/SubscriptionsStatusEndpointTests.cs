using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Subscriptions;
using Awaken.Domain.Entities.Auth;
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

public class SubscriptionsStatusEndpointTests : IAsyncLifetime
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
        var payload = new
        {
            email,
            password = "Str0ngPass!",
            name = "Hunter",
            language = "pt-BR"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.AccessToken;
    }

    private async Task SetTrialEndsAtAsync(string email, DateTime trialEndsAt)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        var subscription = await dbContext.Subscriptions.SingleAsync(s => s.UserId == user.Id);

        dbContext.Entry(user).Property(nameof(User.TrialEndsAt)).CurrentValue = trialEndsAt;
        dbContext.Entry(subscription).Property(nameof(Subscription.TrialEndsAt)).CurrentValue = trialEndsAt;

        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetStatusReturnsNoTrialWhenUserHasNoSubscription()
    {
        var token = await RegisterAndGetTokenAsync("nostatus@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/subscriptions/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionStatusResponse>();
        body!.AccessStatus.Should().Be("no_trial");
        body.TrialStartedAt.Should().BeNull();
        body.TrialEndsAt.Should().BeNull();
        body.DaysRemaining.Should().BeNull();
    }

    [Fact]
    public async Task GetStatusReturnsTrialActiveAfterStartingTrial()
    {
        var token = await RegisterAndGetTokenAsync("activetrial@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsync("/api/subscriptions/trial/start", null);

        var response = await _client.GetAsync("/api/subscriptions/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionStatusResponse>();
        body!.AccessStatus.Should().Be("trial_active");
        body.TrialStartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        body.TrialEndsAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
        body.DaysRemaining.Should().BeInRange(6, 7);
    }

    [Fact]
    public async Task GetStatusReturnsTrialActiveWithThreeDaysRemainingOnCountdownBoundary()
    {
        var token = await RegisterAndGetTokenAsync("countdown@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsync("/api/subscriptions/trial/start", null);
        await SetTrialEndsAtAsync("countdown@awaken.app", DateTime.UtcNow.AddDays(3));

        var response = await _client.GetAsync("/api/subscriptions/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionStatusResponse>();
        body!.AccessStatus.Should().Be("trial_active");
        body.DaysRemaining.Should().Be(3);
    }

    [Fact]
    public async Task GetStatusReturnsTrialExpiredAfterTrialEnds()
    {
        var token = await RegisterAndGetTokenAsync("expired@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsync("/api/subscriptions/trial/start", null);
        await SetTrialEndsAtAsync("expired@awaken.app", DateTime.UtcNow.AddDays(-1));

        var response = await _client.GetAsync("/api/subscriptions/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionStatusResponse>();
        body!.AccessStatus.Should().Be("trial_expired");
        body.DaysRemaining.Should().BeNull();
        body.TrialStartedAt.Should().NotBeNull();
        body.TrialEndsAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStatusIsIdempotentWhenCalledMultipleTimes()
    {
        var token = await RegisterAndGetTokenAsync("idempotent@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsync("/api/subscriptions/trial/start", null);

        var first = await _client.GetAsync("/api/subscriptions/status");
        var second = await _client.GetAsync("/api/subscriptions/status");

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var body1 = await first.Content.ReadFromJsonAsync<SubscriptionStatusResponse>();
        var body2 = await second.Content.ReadFromJsonAsync<SubscriptionStatusResponse>();

        body1!.AccessStatus.Should().Be(body2!.AccessStatus);
    }

    [Fact]
    public async Task GetStatusReturnsUnauthorizedWhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/subscriptions/status");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // US-194: subscription state comes from DB (webhook-driven). Seed it directly for status tests.
    private async Task SeedPaidSubscriptionAsync(
        string email, string plan, DateTime expiresAt, string revenueCatCustomerId = "rc_test")
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
    public async Task GetStatusReturnsSubscriptionActiveWithPlanAfterWebhookActivation()
    {
        var token = await RegisterAndGetTokenAsync("subactive_status@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(30);
        await SeedPaidSubscriptionAsync("subactive_status@awaken.app", "monthly", expiresAt);

        var response = await _client.GetAsync("/api/subscriptions/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionStatusResponse>();
        body!.AccessStatus.Should().Be("subscription_active");
        body.Plan.Should().Be("monthly");
        body.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(2));
        body.DaysRemaining.Should().BeInRange(29, 30);
        body.TrialStartedAt.Should().BeNull();
        body.TrialEndsAt.Should().BeNull();
    }

    [Fact]
    public async Task GetStatusReturnsSubscriptionActiveForAnnualPlan()
    {
        var token = await RegisterAndGetTokenAsync("subactive_annual@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(365);
        await SeedPaidSubscriptionAsync("subactive_annual@awaken.app", "annual", expiresAt);

        var response = await _client.GetAsync("/api/subscriptions/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionStatusResponse>();
        body!.AccessStatus.Should().Be("subscription_active");
        body.Plan.Should().Be("annual");
        body.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task GetStatusPrioritizesPaidPlanOverTrial()
    {
        var token = await RegisterAndGetTokenAsync("subpriority@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Start trial first, then override with a paid subscription via DB seeding.
        await _client.PostAsync("/api/subscriptions/trial/start", null);

        var expiresAt = DateTime.UtcNow.AddDays(30);
        await SeedPaidSubscriptionAsync("subpriority@awaken.app", "monthly", expiresAt);

        var response = await _client.GetAsync("/api/subscriptions/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionStatusResponse>();
        body!.AccessStatus.Should().Be("subscription_active");
        body.Plan.Should().Be("monthly");
    }
}
