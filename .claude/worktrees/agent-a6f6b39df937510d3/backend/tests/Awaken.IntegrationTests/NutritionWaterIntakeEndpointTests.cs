// US-087: POST /api/nutrition/water — registrar consumo de água.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

public class NutritionWaterIntakeEndpointTests : IAsyncLifetime
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
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Str0ngPass!",
            name = "Hunter",
            language = "pt-BR"
        });
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Str0ngPass!"
        });
        return (await loginResponse.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
    }

    private async Task StartTrialAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsJsonAsync("/api/subscriptions/trial/start", new { });
    }

    [Fact]
    public async Task LogWaterIntakeReturnsUnauthorizedWithoutToken()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/nutrition/water", new { amountMl = 250 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogWaterIntakeReturnsForbiddenWhenAccessExpired()
    {
        var token = await RegisterAndGetTokenAsync("nutrition087_blocked@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var user = await db.Users.SingleAsync(u => u.Email == "nutrition087_blocked@awaken.app");
            db.Subscriptions.Add(Subscription.CreateFromPaidPlan(
                user.Id,
                "monthly",
                "pro_access",
                "rc_nutrition087_blocked",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync("/api/nutrition/water", new { amountMl = 250 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LogWaterIntakeReturnsBadRequestForZeroAmount()
    {
        var token = await RegisterAndGetTokenAsync("nutrition087_zero@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/nutrition/water", new { amountMl = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task LogWaterIntakeReturnsBadRequestForNegativeAmount()
    {
        var token = await RegisterAndGetTokenAsync("nutrition087_negative@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/nutrition/water", new { amountMl = -100 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task LogWaterIntakeAdds250MlAndReturnsTotalConsumed()
    {
        var token = await RegisterAndGetTokenAsync("nutrition087_add@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/nutrition/water", new { amountMl = 250 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("waterConsumedMl").GetInt32().Should().Be(250);
    }

    [Fact]
    public async Task LogWaterIntakeAccumulatesMultipleEntries()
    {
        var token = await RegisterAndGetTokenAsync("nutrition087_accumulate@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/nutrition/water", new { amountMl = 250 });
        var response = await _client.PostAsJsonAsync("/api/nutrition/water", new { amountMl = 300 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("waterConsumedMl").GetInt32().Should().Be(550);
    }

    [Fact]
    public async Task LogWaterIntakeUpdatesGetBasicNutritionTodayConsumed()
    {
        var token = await RegisterAndGetTokenAsync("nutrition087_reflect@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/nutrition/water", new { amountMl = 500 });

        var getResponse = await _client.GetAsync("/api/nutrition/basic/today");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("waterConsumedMl").GetInt32().Should().Be(500);
    }

    [Fact]
    public async Task LogWaterIntakeIncludesCorrelationId()
    {
        var token = await RegisterAndGetTokenAsync("nutrition087_corr@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/nutrition/water", new { amountMl = 250 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
