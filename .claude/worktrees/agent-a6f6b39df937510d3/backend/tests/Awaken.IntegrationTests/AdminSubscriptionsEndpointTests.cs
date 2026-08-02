using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Awaken.Contracts.Admin.Subscriptions;
using Awaken.Domain.Entities.Shop;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

/// <summary>
/// US-217: testes de integração do diagnóstico de assinaturas/IAP no admin site.
/// Usa PostgreSQL real via Testcontainers e gera um JWT "AdminSite" manualmente,
/// seguindo o mesmo padrão de AdminSecurityEndpointTests (US-165).
///
/// Cenários de QA cobertos: assinatura aprovada, assinatura negada, IAP aprovado,
/// IAP pendente, transação repetida, falha do provider, usuário com múltiplos eventos.
/// </summary>
public class AdminSubscriptionsEndpointTests : IAsyncLifetime
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

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string GenerateAdminToken()
    {
        var config = _factory.Services.GetRequiredService<IConfiguration>();
        var section = config.GetSection("AdminJwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "AdminSite"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: section["Issuer"],
            audience: section["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void AuthenticateAsAdmin()
    {
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<RevenueCatEvent> SeedRevenueCatEventAsync(
        string eventType = "INITIAL_PURCHASE",
        string? appUserId = null,
        string? originalTransactionId = null,
        string? productId = "plan_monthly",
        DateTime? processedAtUtc = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var rcEvent = RevenueCatEvent.Create(
            eventId: Guid.NewGuid().ToString(),
            appUserId: appUserId ?? Guid.NewGuid().ToString(),
            type: eventType,
            processedAtUtc: processedAtUtc ?? DateTime.UtcNow,
            originalTransactionId: originalTransactionId ?? $"orig-{Guid.NewGuid():N}",
            productId: productId,
            payloadHash: "deadbeef1234abcd");

        db.RevenueCatEvents.Add(rcEvent);
        await db.SaveChangesAsync();
        return rcEvent;
    }

    private async Task<IapTransactionLedger> SeedIapLedgerAsync(
        Guid? userId = null,
        string status = "pending",
        string store = "google_play",
        DateTime? createdAtUtc = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var ledger = IapTransactionLedger.Create(
            userId ?? Guid.NewGuid(), $"tx-{Guid.NewGuid():N}", "gold_pack_small", store,
            createdAtUtc ?? DateTime.UtcNow);

        if (status == "granted") ledger.MarkGranted(DateTime.UtcNow);
        else if (status == "failed") ledger.MarkFailed(DateTime.UtcNow);

        db.IapTransactionLedgers.Add(ledger);
        await db.SaveChangesAsync();
        return ledger;
    }

    // ── Auth ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDiagnostics_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/admin/subscriptions/diagnostics");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetEvents_WithoutAdminRole_Returns403()
    {
        var config = _factory.Services.GetRequiredService<IConfiguration>();
        var section = config.GetSection("AdminJwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()) };
        var token = new JwtSecurityToken(
            issuer: section["Issuer"], audience: section["Audience"], claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30), signingCredentials: creds);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var response = await _client.GetAsync("/api/admin/subscriptions/events");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Cards agregados ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetDiagnostics_SubscriptionApproved_CountsAsApproved()
    {
        await SeedRevenueCatEventAsync(eventType: "INITIAL_PURCHASE");
        AuthenticateAsAdmin();

        var response = await _client.GetAsync("/api/admin/subscriptions/diagnostics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionDiagnosticsResponse>();
        body!.ApprovedCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetDiagnostics_SubscriptionDenied_CountsAsDenied()
    {
        await SeedRevenueCatEventAsync(eventType: "CANCELLATION");
        AuthenticateAsAdmin();

        var response = await _client.GetAsync("/api/admin/subscriptions/diagnostics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionDiagnosticsResponse>();
        body!.DeniedCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetDiagnostics_IapPending_CountsAsPending()
    {
        await SeedIapLedgerAsync(status: "pending");
        AuthenticateAsAdmin();

        var response = await _client.GetAsync("/api/admin/subscriptions/diagnostics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionDiagnosticsResponse>();
        body!.PendingCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetDiagnostics_ProviderFailure_CountsAsFailed()
    {
        await SeedIapLedgerAsync(status: "failed");
        AuthenticateAsAdmin();

        var response = await _client.GetAsync("/api/admin/subscriptions/diagnostics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionDiagnosticsResponse>();
        body!.FailedCount.Should().BeGreaterThanOrEqualTo(1);
    }

    // ── Listagem/filtros ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetEvents_IapApproved_AppearsInList()
    {
        var ledger = await SeedIapLedgerAsync(status: "granted");
        AuthenticateAsAdmin();

        var response = await _client.GetAsync("/api/admin/subscriptions/events?type=iap&status=approved");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionEventListResponse>();
        body!.Items.Should().Contain(i => i.Id == ledger.Id && i.Status == "approved");
    }

    [Fact]
    public async Task GetEvents_FilterByProduct_ReturnsOnlyMatching()
    {
        await SeedRevenueCatEventAsync(productId: "plan_annual");
        await SeedRevenueCatEventAsync(productId: "plan_monthly");
        AuthenticateAsAdmin();

        var response = await _client.GetAsync("/api/admin/subscriptions/events?product=plan_annual");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionEventListResponse>();
        body!.Items.Should().OnlyContain(i => i.Product == "plan_annual");
    }

    [Fact]
    public async Task GetEvents_RepeatedTransaction_IsFlagged()
    {
        var sharedOriginalTxId = $"orig-{Guid.NewGuid():N}";
        await SeedRevenueCatEventAsync(eventType: "INITIAL_PURCHASE", originalTransactionId: sharedOriginalTxId);
        await SeedRevenueCatEventAsync(eventType: "RENEWAL", originalTransactionId: sharedOriginalTxId);
        AuthenticateAsAdmin();

        var response = await _client.GetAsync("/api/admin/subscriptions/events?type=subscription");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionEventListResponse>();
        body!.Items.Where(i => i.MaskedExternalRef != null && i.MaskedExternalRef.EndsWith(sharedOriginalTxId[^4..]))
            .Should().OnlyContain(i => i.IsRepeatedTransaction,
                "RN-005: transações repetidas devem ficar destacadas na listagem");
    }

    [Fact]
    public async Task GetEvents_UserWithMultipleEvents_AllAppearForThatUser()
    {
        var userId = Guid.NewGuid();
        await SeedRevenueCatEventAsync(appUserId: userId.ToString(), eventType: "INITIAL_PURCHASE");
        await SeedRevenueCatEventAsync(appUserId: userId.ToString(), eventType: "RENEWAL");
        await SeedIapLedgerAsync(userId: userId, status: "granted");
        AuthenticateAsAdmin();

        var response = await _client.GetAsync($"/api/admin/subscriptions/events?userId={userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionEventListResponse>();
        body!.Items.Should().HaveCount(3);
        body.Items.Should().OnlyContain(i => i.UserId == userId);
    }

    // ── Detalhe seguro ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetEventDetail_RevenueCatEvent_DoesNotExposeRawPayload()
    {
        var rcEvent = await SeedRevenueCatEventAsync(eventType: "INITIAL_PURCHASE");
        AuthenticateAsAdmin();

        var response = await _client.GetAsync(
            $"/api/admin/subscriptions/events/{rcEvent.Id}?source=revenuecat_event");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionEventDetailResponse>();
        body!.Id.Should().Be(rcEvent.Id);
        body.Status.Should().Be("approved");
        body.MaskedExternalRef.Should().NotBe(rcEvent.OriginalTransactionId,
            "RN-004: referência externa não pode ser exposta crua");
    }

    [Fact]
    public async Task GetEventDetail_WhenEventDoesNotExist_Returns404()
    {
        AuthenticateAsAdmin();

        var response = await _client.GetAsync(
            $"/api/admin/subscriptions/events/{Guid.NewGuid()}?source=revenuecat_event");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEventDetail_IapPending_ReturnsDetailWithPendingStatus()
    {
        var ledger = await SeedIapLedgerAsync(status: "pending");
        AuthenticateAsAdmin();

        var response = await _client.GetAsync(
            $"/api/admin/subscriptions/events/{ledger.Id}?source=iap_ledger");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionEventDetailResponse>();
        body!.Status.Should().Be("pending");
        body.UserId.Should().Be(ledger.UserId);
    }

    [Fact]
    public async Task GetEventDetail_UserWithMultipleEvents_IncludesRelatedEvents()
    {
        var userId = Guid.NewGuid();
        await SeedRevenueCatEventAsync(appUserId: userId.ToString(), eventType: "INITIAL_PURCHASE");
        var second = await SeedRevenueCatEventAsync(appUserId: userId.ToString(), eventType: "RENEWAL");
        AuthenticateAsAdmin();

        var response = await _client.GetAsync(
            $"/api/admin/subscriptions/events/{second.Id}?source=revenuecat_event");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionEventDetailResponse>();
        body!.RelatedUserEvents.Should().NotBeEmpty(
            "RN-005: deve ser possível ver outros eventos do mesmo usuário a partir do detalhe");
    }
}
