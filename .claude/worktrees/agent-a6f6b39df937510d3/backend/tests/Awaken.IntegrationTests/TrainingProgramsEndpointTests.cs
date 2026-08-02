using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.TrainingPrograms;
using Awaken.Domain.Entities.Training;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

/// <summary>
/// Integration tests for US-231/US-232: Training Programs catalog and selection.
/// </summary>
public class TrainingProgramsEndpointTests : IAsyncLifetime
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

    private static string UniqueEmail(string prefix) =>
        $"{prefix}_{Guid.NewGuid():N}@awaken.app";

    private async Task<string> RegisterAndGetTokenAsync(string emailPrefix)
    {
        var email = UniqueEmail(emailPrefix);
        var payload = new { email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Register failed with {(int)response.StatusCode}: {body}");
        }
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        return auth.AccessToken;
    }

    [Fact]
    public async Task GetPrograms_ReturnsSevenPrograms_WithAvailabilityForFreshUserRankE()
    {
        var token = await RegisterAndGetTokenAsync("programs_fresh");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/training-programs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var programs = await response.Content.ReadFromJsonAsync<List<TrainingProgramResponse>>();
        programs.Should().NotBeNull().And.HaveCount(7);

        programs!.Select(p => p.ProgramKey).Should().ContainInOrder(
            TrainingProgramKeys.FullBody,
            TrainingProgramKeys.Ab,
            TrainingProgramKeys.Abc,
            TrainingProgramKeys.Abcd,
            TrainingProgramKeys.Abcde,
            TrainingProgramKeys.Perfect2,
            TrainingProgramKeys.System);

        programs.Single(p => p.ProgramKey == TrainingProgramKeys.FullBody).MinimumRank.Should().Be("E+");
        programs.Single(p => p.ProgramKey == TrainingProgramKeys.Ab).MinimumRank.Should().Be("D+");
        programs.Single(p => p.ProgramKey == TrainingProgramKeys.Abcde).MinimumRank.Should().Be("B+");
        programs.Single(p => p.ProgramKey == TrainingProgramKeys.System).MinimumRank.Should().Be("E+");

        programs!.Single(p => p.ProgramKey == TrainingProgramKeys.FullBody).IsAvailable.Should().BeTrue();
        programs.Single(p => p.ProgramKey == TrainingProgramKeys.System).IsAvailable.Should().BeTrue();

        programs.Single(p => p.ProgramKey == TrainingProgramKeys.Ab).IsAvailable.Should().BeFalse();
        programs.Single(p => p.ProgramKey == TrainingProgramKeys.Abc).IsAvailable.Should().BeFalse();
        programs.Single(p => p.ProgramKey == TrainingProgramKeys.Abcd).IsAvailable.Should().BeFalse();
        programs.Single(p => p.ProgramKey == TrainingProgramKeys.Abcde).IsAvailable.Should().BeFalse();
        programs.Single(p => p.ProgramKey == TrainingProgramKeys.Perfect2).IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task SelectTrainingProgram_Succeeds_WhenRankSufficient()
    {
        var token = await RegisterAndGetTokenAsync("programs_select_ok");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync(
            "/api/users/me/training-program",
            new SelectTrainingProgramRequest(TrainingProgramKeys.FullBody));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync("/api/training-programs");
        var programs = (await getResponse.Content.ReadFromJsonAsync<List<TrainingProgramResponse>>())!;
        programs.Single(p => p.ProgramKey == TrainingProgramKeys.FullBody).IsSelected.Should().BeTrue();
        programs.Where(p => p.ProgramKey != TrainingProgramKeys.FullBody)
            .Should().OnlyContain(p => !p.IsSelected);
    }

    [Fact]
    public async Task SelectTrainingProgram_Returns409_WhenRankInsufficient()
    {
        var token = await RegisterAndGetTokenAsync("programs_select_blocked");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync(
            "/api/users/me/training-program",
            new SelectTrainingProgramRequest(TrainingProgramKeys.Abc));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UnauthenticatedGetPrograms_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/training-programs");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // US-237: split map real, aplicado pela migration de seed contra um Postgres
    // efêmero (Testcontainers) — confirma controller + query + repositório + EF juntos.
    [Fact]
    public async Task GetSplit_ReturnsDaysInOrder_ForAbc()
    {
        var token = await RegisterAndGetTokenAsync("programs_split_abc");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/training-programs/abc/split");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var split = await response.Content.ReadFromJsonAsync<TrainingProgramSplitResponse>();
        split.Should().NotBeNull();
        split!.ProgramKey.Should().Be(TrainingProgramKeys.Abc);
        split.SplitMapVersion.Should().Be("v1");
        split.Days.Select(d => d.DayKey).Should().ContainInOrder("A", "B", "C");
        split.Days.Single(d => d.DayKey == "A").Role.Should().Be("push");
        split.Days.Single(d => d.DayKey == "C").TargetMuscleGroups.Should().Contain("quadriceps");
    }

    // RN-009: programas fora do split clássico (perfect_2/system) não têm split map.
    [Fact]
    public async Task GetSplit_Returns404_ForProgramWithoutClassicSplit()
    {
        var token = await RegisterAndGetTokenAsync("programs_split_none");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/training-programs/{TrainingProgramKeys.Perfect2}/split");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
