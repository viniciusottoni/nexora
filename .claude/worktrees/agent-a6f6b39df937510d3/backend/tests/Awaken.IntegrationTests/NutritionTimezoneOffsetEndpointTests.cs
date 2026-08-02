using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class NutritionTimezoneOffsetEndpointTests : IAsyncLifetime
{
    private sealed class FixedDateTimeService(DateTime utcNow) : IDateTimeService
    {
        public DateTime UtcNow => utcNow;
        public DateOnly TodayUtc => DateOnly.FromDateTime(UtcNow);
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

        var fixedUtcNow = new DateTime(2026, 6, 27, 22, 30, 0, DateTimeKind.Utc);
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
                services.RemoveAll<IDateTimeService>();
                services.AddScoped<IDateTimeService>(_ => new FixedDateTimeService(fixedUtcNow));
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
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsJsonAsync("/api/subscriptions/trial/start", new { });
    }

    [Fact]
    public async Task NutritionLogsUseClientTimezoneOffsetForDayBoundary()
    {
        var token = await RegisterAndGetTokenAsync("nutrition087_timezone@awaken.app");
        await StartTrialAsync(token);

        var post = new HttpRequestMessage(HttpMethod.Post, "/api/nutrition/water");
        post.Headers.Add("X-Timezone-Offset-Minutes", "180");
        post.Content = JsonContent.Create(new { amountMl = 250 });
        var postResponse = await _client.SendAsync(post);

        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var utcResponse = await _client.GetAsync("/api/nutrition/basic/today");
        utcResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var utcDoc = JsonDocument.Parse(await utcResponse.Content.ReadAsStringAsync());
        utcDoc.RootElement.GetProperty("waterConsumedMl").GetInt32().Should().Be(0);

        var localGet = new HttpRequestMessage(HttpMethod.Get, "/api/nutrition/basic/today");
        localGet.Headers.Add("X-Timezone-Offset-Minutes", "180");
        var localResponse = await _client.SendAsync(localGet);

        localResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var localDoc = JsonDocument.Parse(await localResponse.Content.ReadAsStringAsync());
        localDoc.RootElement.GetProperty("waterConsumedMl").GetInt32().Should().Be(250);
    }
}
