using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Media;
using Awaken.Domain.Entities.Exercises;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

/// <summary>
/// US-222: diagnóstico de mídia/CDN responde de forma determinística por exercício, sem depender
/// de rede externa real — evita testes flaky e respeita RN-004 (nunca baixar o binário no teste).
/// </summary>
public class FakeMediaDiagnosticsService : IMediaDiagnosticsService
{
    public Task<MediaAssetDiagnostics> DiagnoseAsync(
        Guid exerciseId, string? imageUrl, string? videoUrl, string? gifUrl,
        CancellationToken cancellationToken = default)
    {
        var image = Classify(imageUrl);
        var video = Classify(videoUrl);
        var gif = Classify(gifUrl);
        return Task.FromResult(new MediaAssetDiagnostics(exerciseId, image, video, gif));
    }

    private static MediaAssetDiagnostic Classify(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return new MediaAssetDiagnostic(MediaAssetStatus.Missing, null, null, null);

        if (url.Contains("broken", StringComparison.OrdinalIgnoreCase))
            return new MediaAssetDiagnostic(MediaAssetStatus.Invalid, 404, 50, null);

        if (url.Contains("slow", StringComparison.OrdinalIgnoreCase))
            return new MediaAssetDiagnostic(MediaAssetStatus.Valid, 200, 3000, false);

        return new MediaAssetDiagnostic(MediaAssetStatus.Valid, 200, 100, true);
    }
}

public class AdminMediaEndpointTests : IAsyncLifetime
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
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IMediaDiagnosticsService, FakeMediaDiagnosticsService>();
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

    private async Task<ExerciseCatalog> SeedExerciseAsync(
        string providerExerciseId,
        string? imageUrl,
        string? videoUrl = null,
        string? gifUrl = null,
        string environment = "home",
        string difficultyLevel = "beginner",
        string equipmentCategory = "bodyweight",
        string primaryRegion = "upper_body")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var snapshot = new ExerciseCatalogSnapshot(
            RawImportId: null,
            ProviderName: "test",
            ProviderExerciseId: providerExerciseId,
            ProviderVersion: null,
            NamePtBr: $"Exercicio {providerExerciseId}",
            NameOriginal: $"Exercise {providerExerciseId}",
            Slug: $"exercicio-{providerExerciseId}",
            DescriptionPtBr: "Descricao",
            InstructionsPtBr: ["Passo 1"],
            InstructionsOriginal: ["Step 1"],
            TipsPtBr: [],
            ExerciseType: "strength",
            MovementPattern: "push",
            MovementFamily: "press",
            Mechanic: "compound",
            ForceType: "push",
            PlaneOfMotion: "sagittal",
            Laterality: "bilateral",
            BodyPosition: "standing",
            BenchAngle: null,
            EquipmentCategory: equipmentCategory,
            LoadType: "bodyweight",
            PrimaryRegion: primaryRegion,
            DifficultyLevel: difficultyLevel,
            DifficultyRank: 1,
            TechnicalComplexity: 1,
            ImpactLevel: 1,
            Environment: environment,
            RequiredEquipment: [],
            PrimaryMuscleGroups: ["chest"],
            SecondaryMuscleGroups: [],
            BodyParts: ["chest"],
            JointStressTags: [],
            ContraindicationTags: [],
            LimitationBlockTags: [],
            PainBlockTags: [],
            GoalTags: [],
            RiskTags: [],
            AccessibilityTags: [],
            TaxonomySignals: [],
            MinExperienceLevel: difficultyLevel,
            SuitableForSedentary: true,
            SuitableForBeginner: true,
            SuitableForIntermediate: true,
            SuitableForAdvanced: true,
            IsCompound: false,
            IsUnilateral: false,
            IsAssisted: false,
            IsWeighted: false,
            RegressionExerciseIds: [],
            ProgressionExerciseIds: [],
            RelatedExerciseIds: [],
            VideoUrl: videoUrl,
            ImageUrl: imageUrl,
            GifUrl: gifUrl,
            MediaLicenseInfo: null,
            SanitizationStatus: "approved",
            IsApprovedForWorkoutGeneration: true,
            Confidence: "high");

        var exercise = ExerciseCatalog.Create(snapshot, DateTime.UtcNow);
        db.ExerciseCatalogs.Add(exercise);
        await db.SaveChangesAsync();
        return exercise;
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCoverage_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/admin/media/coverage");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCoverage_WhenAdmin_ReturnsAggregatedCoverage()
    {
        await SeedExerciseAsync("cov-1", imageUrl: "https://cdn.awaken.app/cov-1.jpg");
        await SeedExerciseAsync("cov-2", imageUrl: null);

        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/media/coverage");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MediaCoverageResponse>();
        body!.TotalExercises.Should().Be(2);
        body.PercentWithImage.Should().Be(50);
    }

    [Fact]
    public async Task GetExercises_ExerciseWithValidImage_AppearsAsOk()
    {
        await SeedExerciseAsync("valid-1", imageUrl: "https://cdn.awaken.app/valid-1.jpg");

        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/media/exercises?mediaStatus=ok");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MediaDiagnosticsListResponse>();
        body!.Items.Should().Contain(i => i.Slug == "exercicio-valid-1" && i.MediaStatus == "ok");
    }

    [Fact]
    public async Task GetExercises_ExerciseWithoutMedia_AppearsAsMissing()
    {
        await SeedExerciseAsync("no-media-1", imageUrl: null);

        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/media/exercises?mediaStatus=missing");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MediaDiagnosticsListResponse>();
        body!.Items.Should().Contain(i => i.Slug == "exercicio-no-media-1" && i.MediaStatus == "missing");
    }

    [Fact]
    public async Task GetExercises_ExerciseWithInvalidUrl_AppearsAsInvalidLink()
    {
        // RN-002: a URL é propositalmente inválida — o FakeMediaDiagnosticsService classifica
        // qualquer URL contendo "broken" como Invalid, sem qualquer chamada de rede real.
        await SeedExerciseAsync("broken-1", imageUrl: "https://cdn.awaken.app/broken-asset.jpg");

        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/media/exercises?mediaStatus=invalid_link");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MediaDiagnosticsListResponse>();
        body!.Items.Should().Contain(i => i.Slug == "exercicio-broken-1" && i.MediaStatus == "invalid_link");
    }

    [Fact]
    public async Task GetExercises_SlowAsset_AppearsAsSlow()
    {
        await SeedExerciseAsync("slow-1", imageUrl: "https://cdn.awaken.app/slow-asset.jpg");

        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/media/exercises?mediaStatus=slow");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MediaDiagnosticsListResponse>();
        body!.Items.Should().Contain(i => i.Slug == "exercicio-slow-1" && i.MediaStatus == "slow");
    }

    [Fact]
    public async Task GetExercises_FilterByEnvironment_ReturnsOnlyMatching()
    {
        await SeedExerciseAsync("home-ex", imageUrl: "https://cdn.awaken.app/home.jpg", environment: "home");
        await SeedExerciseAsync("gym-ex", imageUrl: "https://cdn.awaken.app/gym.jpg", environment: "gym");

        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/media/exercises?environment=gym");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MediaDiagnosticsListResponse>();
        body!.Items.Should().OnlyContain(i => i.Environment == "gym");
        body.Items.Should().Contain(i => i.Slug == "exercicio-gym-ex");
    }

    [Fact]
    public async Task GetExercises_WithoutAdminRole_Returns403()
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
        var response = await _client.GetAsync("/api/admin/media/exercises");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetExercises_ResponseBody_NeverContainsStorageCredentialKeys()
    {
        // RN-005: garante que a resposta JSON crua não vaza nenhum termo de credencial de storage.
        await SeedExerciseAsync("safe-1", imageUrl: "https://cdn.awaken.app/safe-1.jpg");

        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/media/exercises");
        var rawJson = await response.Content.ReadAsStringAsync();

        rawJson.Should().NotContainEquivalentOf("accountId");
        rawJson.Should().NotContainEquivalentOf("secretKey");
        rawJson.Should().NotContainEquivalentOf("accessKey");
        rawJson.Should().NotContainEquivalentOf("r2AccountId");
    }
}
