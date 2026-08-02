// US-054: salvar preferencia de tipo de treino (upsert, 204).
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
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

public class SaveWorkoutTypePreferenceEndpointTests : IAsyncLifetime
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

    private async Task StartTrialAsync()
    {
        var response = await _client.PostAsync("/api/subscriptions/trial/start", null);
        response.EnsureSuccessStatusCode();
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

    // ── CA-001: salva e persiste ──────────────────────────────────────────────

    [Fact]
    public async Task CA001_Returns204_AndPersistsPreference()
    {
        var token = await RegisterAndGetTokenAsync("pref-save@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/users/me/workout-preferences/training-type",
            new { preferredTrainingType = "program", preferredProgramId = "perfect_2" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var saved = await db.UserWorkoutPreferences.AsNoTracking().SingleAsync();
        saved.PreferredTrainingType.Should().Be("program");
        saved.PreferredProgramId.Should().Be("perfect_2");
    }

    // ── Upsert: segunda chamada atualiza, nao duplica ─────────────────────────

    [Fact]
    public async Task Upsert_SecondCallUpdatesSingleRow()
    {
        var token = await RegisterAndGetTokenAsync("pref-upsert@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();

        await _client.PostAsJsonAsync(
            "/api/users/me/workout-preferences/training-type",
            new { preferredTrainingType = "regeneration" });

        var second = await _client.PostAsJsonAsync(
            "/api/users/me/workout-preferences/training-type",
            new { preferredTrainingType = "program", preferredProgramId = "saitama_path" });

        second.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var rows = await db.UserWorkoutPreferences.AsNoTracking().ToListAsync();
        rows.Should().HaveCount(1);
        rows[0].PreferredTrainingType.Should().Be("program");
        rows[0].PreferredProgramId.Should().Be("saitama_path");
    }

    // ── Validacao: tipo invalido → 422 ────────────────────────────────────────

    [Fact]
    public async Task Returns422_WhenTypeInvalid()
    {
        var token = await RegisterAndGetTokenAsync("pref-invalid@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/users/me/workout-preferences/training-type",
            new { preferredTrainingType = "free_edit" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── RN-006: acesso expirado → 403 ─────────────────────────────────────────

    [Fact]
    public async Task RN006_Returns403_WhenAccessExpired()
    {
        var token = await RegisterAndGetTokenAsync("pref-expired@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(-1);
        await SeedPaidSubscriptionAsync("pref-expired@awaken.app", "monthly", expiresAt, "rc_pref_expired");

        var response = await _client.PostAsJsonAsync(
            "/api/users/me/workout-preferences/training-type",
            new { preferredTrainingType = "regeneration" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("ACCESS_BLOCKED");
    }

    [Fact]
    public async Task Returns401_WhenUnauthenticated()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/users/me/workout-preferences/training-type",
            new { preferredTrainingType = "regeneration" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
