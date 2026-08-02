// US-020: backend bloqueia endpoints protegidos para usuários com acesso expirado.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Legal;
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

public class AccessBlockedEndpointTests : IAsyncLifetime
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
    public async Task SubscriptionExpiredUserIsBlockedFromProtectedPath()
    {
        var token = await RegisterAndGetTokenAsync("blocked_sub@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(-1);
        await SeedPaidSubscriptionAsync("blocked_sub@awaken.app", "monthly", expiresAt, "rc_blocked_test");

        var response = await _client.GetAsync("/api/quests");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SubscriptionExpiredUserBlockedResponseContainsAccessBlockedCode()
    {
        var token = await RegisterAndGetTokenAsync("blocked_code@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(-1);
        await SeedPaidSubscriptionAsync("blocked_code@awaken.app", "monthly", expiresAt, "rc_blocked_code_test");

        var response = await _client.GetAsync("/api/quests");
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        body.Should().ContainKey("code");
        body!["code"].ToString().Should().Be("ACCESS_BLOCKED");
        body.Should().ContainKey("accessStatus");
        body["accessStatus"].ToString().Should().Be("subscription_expired");
    }

    [Fact]
    public async Task SubscriptionExpiredUserCanStillAccessSubscriptionStatusEndpoint()
    {
        var token = await RegisterAndGetTokenAsync("blocked_allow@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(-1);
        await SeedPaidSubscriptionAsync("blocked_allow@awaken.app", "monthly", expiresAt, "rc_blocked_allow_test");

        var response = await _client.GetAsync("/api/subscriptions/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionStatusResponse>();
        body!.AccessStatus.Should().Be("subscription_expired");
    }

    [Fact]
    public async Task SubscriptionExpiredUserCanStillAcceptLegalTerms()
    {
        var token = await RegisterAndGetTokenAsync("blocked_legal@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(-1);
        await SeedPaidSubscriptionAsync("blocked_legal@awaken.app", "monthly", expiresAt, "rc_blocked_legal_test");

        var response = await _client.PostAsJsonAsync("/api/users/me/legal-acceptance", new
        {
            termsVersion = "1.0.0",
            privacyVersion = "1.0.0",
            accepted = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LegalStatusResponse>();
        body!.HasAcceptedLegal.Should().BeTrue();
        body.TermsAcceptedAt.Should().NotBeNull();
        body.PrivacyAcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ActiveSubscriptionUserPassesThroughMiddlewareToRouter()
    {
        var token = await RegisterAndGetTokenAsync("active_sub@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(30);
        await SeedPaidSubscriptionAsync("active_sub@awaken.app", "monthly", expiresAt, "rc_active_test");

        // Middleware deixa passar; router retorna 404 pois /api/quests não existe ainda
        var response = await _client.GetAsync("/api/quests");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnauthenticatedRequestIsNotInterceptedByActiveAccessMiddleware()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        // Middleware ignora requisições sem autenticação; JWT retorna 401
        var response = await _client.GetAsync("/api/subscriptions/status");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
