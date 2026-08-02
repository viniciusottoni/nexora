using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Common;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class UsersPhysicalDataEndpointTests : IAsyncLifetime
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

    private async Task<string> RegisterAndGetTokenAsync(string email = "physical@awaken.app")
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Str0ngPass!",
            name = "Hunter",
            language = "pt-BR"
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.AccessToken;
    }

    [Fact]
    public async Task CA001_ValidDataIsSaved()
    {
        var token = await RegisterAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            age = 28,
            heightCm = 175.0,
            weightKg = 82.0,
            biologicalSex = "masculino",
            trainingDuration = "1_6_months",
            availableMinutesPerWorkout = 30
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var profile = await dbContext.UserProfiles.SingleAsync();
        profile.TrainingDuration.Should().Be("1_6_months");
        profile.AvailableMinutesPerWorkout.Should().Be(30);
    }

    [Fact]
    public async Task CA002_FreeTextBiologicalSexIsAccepted()
    {
        var token = await RegisterAndGetTokenAsync("freetext@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            age = 25,
            heightCm = 165.0,
            weightKg = 60.0,
            biologicalSex = "nao-binario"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task US140_AllowsSavingOnlyTrainingDuration()
    {
        var token = await RegisterAndGetTokenAsync("trainingduration@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            trainingDuration = "more_than_3_years"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var profile = await dbContext.UserProfiles.SingleAsync();
        profile.TrainingDuration.Should().Be("more_than_3_years");
        profile.AvailableMinutesPerWorkout.Should().BeNull();
        profile.Age.Should().BeNull();
    }

    [Fact]
    public async Task US028_AllowsSavingOnlyAvailableMinutes()
    {
        var token = await RegisterAndGetTokenAsync("availableminutes@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            availableMinutesPerWorkout = 40
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var profile = await dbContext.UserProfiles.SingleAsync();
        profile.AvailableMinutesPerWorkout.Should().Be(40);
        profile.TrainingDuration.Should().BeNull();
    }

    [Fact]
    public async Task ValidationFails_WhenAgeOutOfRange()
    {
        var token = await RegisterAndGetTokenAsync("agefail@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            age = 5,
            heightCm = 170.0,
            weightKg = 70.0,
            biologicalSex = "feminino"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Code.Should().Be("INVALID_PROFILE_DATA");
        error.Message.Should().Be("Revise os dados informados.");
    }

    [Fact]
    public async Task ValidationFails_WhenHeightOutOfRange()
    {
        var token = await RegisterAndGetTokenAsync("heightfail@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            age = 30,
            heightCm = 20.0,
            weightKg = 70.0,
            biologicalSex = "masculino"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ValidationFails_WhenWeightOutOfRange()
    {
        var token = await RegisterAndGetTokenAsync("weightfail@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            age = 30,
            heightCm = 170.0,
            weightKg = 5.0,
            biologicalSex = "feminino"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ValidationFails_WhenBiologicalSexIsEmpty()
    {
        var token = await RegisterAndGetTokenAsync("sexfail@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            age = 30,
            heightCm = 170.0,
            weightKg = 70.0,
            biologicalSex = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ValidationFails_WhenTrainingDurationIsInvalid()
    {
        var token = await RegisterAndGetTokenAsync("trainingdurationfail@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            trainingDuration = "2_years"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ValidationFails_WhenAvailableMinutesIsInvalid()
    {
        var token = await RegisterAndGetTokenAsync("availableminutesfail@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            availableMinutesPerWorkout = 15
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ValidationFails_WhenRequestIsEmpty()
    {
        var token = await RegisterAndGetTokenAsync("emptyrequest@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new { });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task US141_AllowsSavingOnlyBodyType()
    {
        var token = await RegisterAndGetTokenAsync("bodytype@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            bodyType = "athletic_strong"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var profile = await dbContext.UserProfiles.SingleAsync();
        profile.BodyType.Should().Be("athletic_strong");
        profile.Age.Should().BeNull();
        profile.TrainingDuration.Should().BeNull();
    }

    [Theory]
    [InlineData("lean")]
    [InlineData("normal")]
    [InlineData("overweight")]
    [InlineData("athletic_strong")]
    public async Task US141_AllAllowedBodyTypesAreAccepted(string bodyType)
    {
        var token = await RegisterAndGetTokenAsync($"bodytype_{bodyType}@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            bodyType
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task US141_ValidationFails_WhenBodyTypeIsInvalid()
    {
        var token = await RegisterAndGetTokenAsync("bodytypefail@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            bodyType = "obese"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task US142_AllowsSavingOnlyPhysicalLimitations()
    {
        var token = await RegisterAndGetTokenAsync("limitations@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            physicalLimitations = new[] { "knee_problem", "no_impact" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var profile = await dbContext.UserProfiles.SingleAsync();
        profile.PhysicalLimitations.Should().BeEquivalentTo(new[] { "knee_problem", "no_impact" });
        profile.Age.Should().BeNull();
    }

    [Fact]
    public async Task US142_NoLimitationsTagIsSaved()
    {
        var token = await RegisterAndGetTokenAsync("nolimitations@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            physicalLimitations = new[] { "no_limitations" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var profile = await dbContext.UserProfiles.SingleAsync();
        profile.PhysicalLimitations.Should().ContainSingle().Which.Should().Be("no_limitations");
    }

    [Fact]
    public async Task US142_ValidationFails_WhenPhysicalLimitationsContainsInvalidTag()
    {
        var token = await RegisterAndGetTokenAsync("limitationsfail@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            physicalLimitations = new[] { "unknown_tag" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task US142_ValidationFails_WhenPhysicalLimitationsIsEmpty()
    {
        var token = await RegisterAndGetTokenAsync("limitationsempty@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            physicalLimitations = Array.Empty<string>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData("no_limitations")]
    [InlineData("disk_herniation")]
    [InlineData("knee_problem")]
    [InlineData("no_impact")]
    [InlineData("shoulder_injury")]
    [InlineData("chronic_lumbar_pain")]
    [InlineData("medical_restriction")]
    public async Task US142_AllAllowedLimitationTagsAreAccepted(string tag)
    {
        var token = await RegisterAndGetTokenAsync($"limitation_{tag}@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            physicalLimitations = new[] { tag }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task US030_AllowsSavingOnlyPhysicalPains()
    {
        var token = await RegisterAndGetTokenAsync("physicalpains@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            physicalPains = new[] { "neck", "lower_back" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var profile = await dbContext.UserProfiles.SingleAsync();
        profile.PhysicalPains.Should().BeEquivalentTo(new[] { "neck", "lower_back" });
        profile.Age.Should().BeNull();
    }

    [Theory]
    [InlineData("no_pains")]
    [InlineData("neck")]
    [InlineData("shoulder")]
    [InlineData("wrist")]
    [InlineData("back")]
    [InlineData("lower_back")]
    [InlineData("knees")]
    public async Task US030_AllAllowedPainTagsAreAccepted(string tag)
    {
        var token = await RegisterAndGetTokenAsync($"pain_{tag}@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            physicalPains = new[] { tag }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task US030_ValidationFails_WhenPhysicalPainsContainsInvalidTag()
    {
        var token = await RegisterAndGetTokenAsync("painsinvalidtag@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            physicalPains = new[] { "unknown_pain" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task US030_ValidationFails_WhenPhysicalPainsIsEmpty()
    {
        var token = await RegisterAndGetTokenAsync("painsempty@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            physicalPains = Array.Empty<string>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task US030_MultipleValidPainTagsAreAccepted()
    {
        var token = await RegisterAndGetTokenAsync("multiplepains@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            physicalPains = new[] { "neck", "shoulder", "knees" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            age = 28,
            heightCm = 175.0,
            weightKg = 82.0,
            biologicalSex = "masculino"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SavePhysicalData_IsIdempotent_UpdatesExistingProfile()
    {
        var token = await RegisterAndGetTokenAsync("idempotent@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var firstResponse = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            trainingDuration = "does_not_train",
            availableMinutesPerWorkout = 10
        });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var secondResponse = await _client.PatchAsJsonAsync("/api/users/me/profile/onboarding", new
        {
            age = 29,
            heightCm = 176.0,
            weightKg = 83.0,
            biologicalSex = "masculino",
            trainingDuration = "1_6_months",
            availableMinutesPerWorkout = 50
        });
        secondResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var profile = await dbContext.UserProfiles.SingleAsync();
        profile.Age.Should().Be(29);
        profile.HeightCm.Should().Be(176m);
        profile.WeightKg.Should().Be(83m);
        profile.BiologicalSex.Should().Be("masculino");
        profile.TrainingDuration.Should().Be("1_6_months");
        profile.AvailableMinutesPerWorkout.Should().Be(50);
    }
}
