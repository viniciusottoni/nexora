using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class AuditLogIntegrationTests : IAsyncLifetime
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

    private async Task<(string token, Guid userId)> RegisterAndGetTokenAsync(string email)
    {
        var payload = new { email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        return (auth.AccessToken, auth.User.Id);
    }

    private async Task<int> CountAuditLogsAsync(string action, Guid? actorUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        return await db.AuditLogs
            .Where(a => a.Action == action && a.ActorUserId == actorUserId)
            .CountAsync();
    }

    [Fact]
    public async Task AcceptLegalTermsCreatesAuditLogEntry()
    {
        var (token, userId) = await RegisterAndGetTokenAsync("audit_legal@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/users/me/legal-acceptance", new
        {
            accepted = true,
            termsVersion = "v1.0",
            privacyVersion = "v1.0"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var count = await CountAuditLogsAsync("legal_terms_accepted", userId);
        count.Should().Be(1);
    }

    [Fact]
    public async Task AcceptResponsibilityNoticeCreatesAuditLogEntry()
    {
        var (token, userId) = await RegisterAndGetTokenAsync("audit_notice@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/users/me/responsibility-notice", new
        {
            accepted = true,
            noticeVersion = "v1.0"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var count = await CountAuditLogsAsync("responsibility_notice_accepted", userId);
        count.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAccountCreatesAuditLogEntry()
    {
        var (token, userId) = await RegisterAndGetTokenAsync("audit_delete@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/users/me/delete-account", new
        {
            confirmation = "DELETE_MY_ACCOUNT"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var count = await CountAuditLogsAsync("account_deleted", userId);
        count.Should().Be(1);
    }

    [Fact]
    public async Task AuditLogDoesNotContainSensitivePersonalData()
    {
        var (token, userId) = await RegisterAndGetTokenAsync("audit_safe@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/users/me/legal-acceptance", new
        {
            accepted = true,
            termsVersion = "v1.0",
            privacyVersion = "v1.0"
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var entry = await db.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "legal_terms_accepted" && a.ActorUserId == userId);

        entry.Should().NotBeNull();
        entry!.MetadataSafe.Should().NotContain("Str0ngPass!");
        entry.MetadataSafe.Should().NotContain("audit_safe@awaken.app");
    }

    [Fact]
    public async Task AuditLogContainsCorrelationId()
    {
        var (token, userId) = await RegisterAndGetTokenAsync("audit_correlation@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/users/me/legal-acceptance", new
        {
            accepted = true,
            termsVersion = "v1.0",
            privacyVersion = "v1.0"
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var entry = await db.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "legal_terms_accepted" && a.ActorUserId == userId);

        entry.Should().NotBeNull();
        entry!.CorrelationId.Should().NotBeNullOrWhiteSpace();
    }
}
