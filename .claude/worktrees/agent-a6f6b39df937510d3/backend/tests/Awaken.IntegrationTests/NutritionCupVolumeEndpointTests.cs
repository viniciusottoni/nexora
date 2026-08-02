// US-090: PATCH /api/nutrition/preferences/cup-volume — volume preferido do copo.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

public class NutritionCupVolumeEndpointTests : IAsyncLifetime
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
    public async Task UpdateCupVolumeReturnsUnauthorizedWithoutToken()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 300 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateCupVolumeReturnsBadRequestForValueBelowMinimum()
    {
        var token = await RegisterAndGetTokenAsync("nutrition090_below@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 49 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateCupVolumeReturnsBadRequestForValueAboveMaximum()
    {
        var token = await RegisterAndGetTokenAsync("nutrition090_above@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 2001 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateCupVolumePersistsPreference()
    {
        var token = await RegisterAndGetTokenAsync("nutrition090_persist@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var patchResponse = await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 500 });
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await patchResponse.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("cupVolumeMl").GetInt32().Should().Be(500);
    }

    [Fact]
    public async Task UpdateCupVolumeReflectsInGetBasicNutritionToday()
    {
        var token = await RegisterAndGetTokenAsync("nutrition090_reflect@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 350 });

        var getResponse = await _client.GetAsync("/api/nutrition/basic/today");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("cupVolumeMl").GetInt32().Should().Be(350);
    }

    [Fact]
    public async Task GetBasicNutritionTodayDefaultsCupVolumeToTwoFifty()
    {
        var token = await RegisterAndGetTokenAsync("nutrition090_default@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var getResponse = await _client.GetAsync("/api/nutrition/basic/today");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("cupVolumeMl").GetInt32().Should().Be(250);
    }

    [Fact]
    public async Task UpdateCupVolumeOverwritesPreviousPreference()
    {
        var token = await RegisterAndGetTokenAsync("nutrition090_overwrite@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 200 });
        var second = await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 400 });

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("cupVolumeMl").GetInt32().Should().Be(400);
    }

    [Fact]
    public async Task UpdateCupVolumeIncludesCorrelationId()
    {
        var token = await RegisterAndGetTokenAsync("nutrition090_corr@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 300 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CA001_RecalculatesCupsWhenVolumeChanges()
    {
        // US-090 CA-001: 1000 ml consumido; cliente recalcula copos com novo volume.
        var token = await RegisterAndGetTokenAsync("nutrition090_ca001@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/nutrition/water", new { amountMl = 500 });
        await _client.PostAsJsonAsync("/api/nutrition/water", new { amountMl = 500 });

        await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 500 });

        var response = await _client.GetAsync("/api/nutrition/basic/today");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("waterConsumedMl").GetInt32().Should().Be(1000);
        doc.RootElement.GetProperty("cupVolumeMl").GetInt32().Should().Be(500);
    }
}
