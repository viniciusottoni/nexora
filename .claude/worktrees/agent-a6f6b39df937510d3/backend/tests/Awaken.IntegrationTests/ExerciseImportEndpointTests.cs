using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Exercises;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

/// <summary>
/// US-236 — fake determinístico de <see cref="IMediaStorageService"/> para os testes de integração do
/// import não dependerem de credenciais reais do bucket S3-compatível (que não existem neste ambiente) nem de
/// rede externa — evita testes flaky e respeita a orientação de nunca usar credenciais reais em teste.
/// </summary>
public class FakeMediaStorageService : IMediaStorageService
{
    public Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default) =>
        Task.FromResult($"https://media.awaken.test/api/media/{key}");
}

public class ExerciseImportEndpointTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("awaken")
        .WithUsername("awaken")
        .WithPassword("awaken_test_password")
        .Build();

    // Root directory configured on the server side
    private readonly string _importRootDirectory = Path.Combine(
        Path.GetTempPath(),
        $"awaken-exercises-root-{Guid.NewGuid():N}");

    // The batch key (relative subdirectory)
    private const string BatchKey = "batch-2026-01";
    private string SignedEulaPath => Path.Combine(_importRootDirectory, "ExerciseDB_EULA_signed.pdf");

    // Physical path to the batch: root/batchKey
    private string BatchDirectory => Path.Combine(_importRootDirectory, BatchKey);

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(BatchDirectory);
        await File.WriteAllBytesAsync(SignedEulaPath, [37, 80, 68, 70]);
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                // ExerciseImport:RootDirectory precisa vir daqui (não de builder.UseSetting):
                // appsettings.json define "" explicitamente e é carregado depois dos webHost
                // settings, sobrescrevendo UseSetting de volta para "" (SafeDirectoryResolver
                // sempre resolveria null). AddInMemoryCollection via ConfigureAppConfiguration
                // é adicionado por último e vence.
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PostgreSQL"] = _postgres.GetConnectionString(),
                    ["ExerciseImport:RootDirectory"] = _importRootDirectory,
                    ["ExerciseImport:SignedEulaPath"] = SignedEulaPath,
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IMediaStorageService, FakeMediaStorageService>();
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

        if (Directory.Exists(_importRootDirectory))
            Directory.Delete(_importRootDirectory, recursive: true);
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        var email = $"admin-import-{Guid.NewGuid():N}@awaken.app";
        const string password = "Str0ngPass!";

        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            name = "Admin",
            language = "pt-BR"
        });

        // Pré-existente: /api/admin/exercises/import exige a policy "Admin" (role "Admin" no JWT),
        // que o registro comum nunca concede — precisa ser elevado direto no banco, igual ao padrão
        // já usado em AdminAuthorizationTests.RegisterAdminAndLoginAsync.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var user = await db.Users.FirstAsync(u => u.Email == email);
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE users SET \"Role\" = 'Admin' WHERE \"Id\" = {0}", user.Id);
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.AccessToken;
    }

    private HttpRequestMessage ImportRequest(
        string accessToken,
        string batchKey = BatchKey,
        bool approveOnImport = false,
        string? datasetVersion = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/exercises/import")
        {
            Content = JsonContent.Create(new ImportExercisesRequest(
                batchKey,
                provider: "local_files",
                maxFiles: null,
                approveOnImport,
                datasetVersion))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    [Fact]
    public async Task ImportSavesRawJsonAndCatalogWithRelativeMediaKeyWhenAuthenticated()
    {
        var accessToken = await RegisterAndLoginAsync();
        WriteSampleExercise("0025");

        var response = await _client.SendAsync(ImportRequest(accessToken));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ImportExercisesResponse>();
        result!.RawImported.Should().Be(1);
        result.CatalogCreated.Should().Be(1);
        result.Failed.Should().Be(0);
        result.ImportBatchId.Should().NotBeNullOrWhiteSpace();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var savedRaw = await dbContext.ExerciseRawImports.FirstAsync(e => e.ProviderExerciseId == "0025");
        savedRaw.RawJson.Should().Contain("\"barbell bench press\"");
        savedRaw.ProviderName.Should().Be("local_files");
        savedRaw.SourceFilePath.Should().Be($"{BatchKey}/0025.json");
        savedRaw.MediaBaseUrl.Should().Be("https://media.awaken.test/api/media/exercises/0025/360.gif");
        savedRaw.ImportBatchId.Should().Be(result.ImportBatchId);

        // Physical path must NOT be stored in the database
        savedRaw.SourceFilePath.Should().NotContain(_importRootDirectory);
        savedRaw.MediaBaseUrl.Should().NotContain(_importRootDirectory);

        var savedCatalog = await dbContext.ExerciseCatalogs
            .Include(e => e.AttributeContribution)
            .Include(e => e.Relations)
            .Include(e => e.Taxonomy)
            .FirstAsync(e => e.ProviderExerciseId == "0025");
        savedCatalog.MovementPattern.Should().Be("horizontal_push");
        savedCatalog.RequiredEquipment.Should().Contain("barbell");
        savedCatalog.PrimaryMuscleGroups.Should().Contain("pectorals");
        savedCatalog.GifUrl.Should().Be("https://media.awaken.test/api/media/exercises/0025/360.gif");
        savedCatalog.GifUrl.Should().NotContain(_importRootDirectory);
        savedCatalog.AttributeContribution!.WisdomXp.Should().Be(1);
        savedCatalog.Relations.Should().Contain(relation => relation.RelationKind == "progression");
    }

    [Fact]
    public async Task ImportDoesNotDuplicateRawOrCatalogWhenSameLocalFileIsReimported()
    {
        var accessToken = await RegisterAndLoginAsync();
        WriteSampleExercise("0025");

        await _client.SendAsync(ImportRequest(accessToken));
        await _client.SendAsync(ImportRequest(accessToken));

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var rawCount = await dbContext.ExerciseRawImports.CountAsync(e => e.ProviderExerciseId == "0025");
        var catalogCount = await dbContext.ExerciseCatalogs.CountAsync(e => e.ProviderExerciseId == "0025");
        rawCount.Should().Be(1);
        catalogCount.Should().Be(1);
    }

    [Fact]
    public async Task EnrichedImportUpdatesExistingCatalogAndResolvesRelationshipTarget()
    {
        var accessToken = await RegisterAndLoginAsync();
        WriteSampleExercise("0025");
        WriteSampleExercise("0289");

        await _client.SendAsync(ImportRequest(accessToken));
        var enrichedResponse = await _client.SendAsync(
            ImportRequest(accessToken, datasetVersion: "051426"));

        enrichedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await enrichedResponse.Content.ReadFromJsonAsync<ImportExercisesResponse>();
        result!.CatalogCreated.Should().Be(0);
        result.CatalogUpdated.Should().Be(2);
        result.TaxonomyApplied.Should().Be(2);
        result.RelationshipsCreated.Should().Be(8);
        result.Pending.Should().Be(0);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var target = await db.ExerciseCatalogs.SingleAsync(e => e.ProviderExerciseId == "0289");
        var source = await db.ExerciseCatalogs
            .Include(e => e.Relations)
            .SingleAsync(e => e.ProviderExerciseId == "0025");
        source.Relations.Should().Contain(relation =>
            relation.RelatedProviderExerciseId == "0289"
            && relation.TargetExerciseCatalogId == target.Id);

        var raw = await db.ExerciseRawImports.SingleAsync(e => e.ProviderExerciseId == "0025");
        raw.DatasetVersion.Should().Be("051426");
    }

    [Fact]
    public async Task ImportCanApproveCatalogWhenRequested()
    {
        var accessToken = await RegisterAndLoginAsync();
        WriteSampleExercise("0025");

        var response = await _client.SendAsync(ImportRequest(accessToken, approveOnImport: true));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var savedCatalog = await dbContext.ExerciseCatalogs.FirstAsync(e => e.ProviderExerciseId == "0025");
        savedCatalog.IsApprovedForWorkoutGeneration.Should().BeTrue();
        savedCatalog.SanitizationStatus.Should().Be("approved");
    }

    [Fact]
    public async Task ImportReturnsUnauthorizedWithoutToken()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/exercises/import")
        {
            Content = JsonContent.Create(new ImportExercisesRequest(BatchKey, "local_files"))
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ImportReturnsBadRequestWhenBatchKeyIsAbsolutePath()
    {
        var accessToken = await RegisterAndLoginAsync();

        var response = await _client.SendAsync(
            ImportRequest(accessToken, batchKey: @"C:\Windows\System32"));

        // Pré-existente: ValidationException é mapeada para 422 (nao 400) por ExceptionHandlingMiddleware,
        // igual a todos os outros testes de validacao do projeto — o teste esperava o codigo errado.
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ImportReturnsBadRequestWhenBatchKeyContainsTraversal()
    {
        var accessToken = await RegisterAndLoginAsync();

        var response = await _client.SendAsync(
            ImportRequest(accessToken, batchKey: "../../etc/passwd"));

        // Pré-existente: ValidationException é mapeada para 422 (nao 400) por ExceptionHandlingMiddleware,
        // igual a todos os outros testes de validacao do projeto — o teste esperava o codigo errado.
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ImportReturnsBadRequestWhenBatchKeyContainsDotDot()
    {
        var accessToken = await RegisterAndLoginAsync();

        var response = await _client.SendAsync(
            ImportRequest(accessToken, batchKey: "../secret"));

        // Pré-existente: ValidationException é mapeada para 422 (nao 400) por ExceptionHandlingMiddleware,
        // igual a todos os outros testes de validacao do projeto — o teste esperava o codigo errado.
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private void WriteSampleExercise(string id)
    {
        File.WriteAllText(Path.Combine(BatchDirectory, $"{id}.json"), SampleExerciseJson(id));
        File.WriteAllBytes(Path.Combine(BatchDirectory, $"{id}-360.gif"), [71, 73, 70, 56]);
    }

    private static string SampleExerciseJson(string id) => $$"""
    {
      "bodyPart": "chest",
      "equipment": "barbell",
      "id": "{{id}}",
      "name": "barbell bench press",
      "target": "pectorals",
      "secondaryMuscles": ["triceps", "shoulders"],
      "instructions": ["Lie flat on a bench.", "Press the barbell up."],
      "description": "Classic compound chest exercise.",
      "difficulty": "intermediate",
      "category": "strength",
      "taxonomy": {
        "movementFamily": "bench press",
        "movementPattern": "horizontal push",
        "mechanic": "compound",
        "forceType": "push",
        "planeOfMotion": "sagittal",
        "laterality": "bilateral",
        "bodyPosition": "lying",
        "benchAngle": "flat",
        "equipmentCategory": "free_weight",
        "loadType": "free_weight",
        "primaryRegion": "upper_body",
        "isCompound": true,
        "isUnilateral": false,
        "isAssisted": false,
        "isWeighted": true,
        "signals": ["external_load", "free_weight"],
        "confidence": "high"
      },
      "similarExercises": [
        { "id": "0033", "name": "barbell decline bench press", "score": 100.0, "confidence": "high", "reasons": ["same target muscle"] }
      ],
      "substitutions": [
        { "id": "0289", "name": "dumbbell bench press", "types": ["equipment_alternative"], "score": 100.0, "confidence": "high", "reasons": ["different equipment option"] }
      ],
      "progressions": [
        { "id": "0045", "name": "barbell guillotine bench press", "types": ["higher_difficulty"], "score": 100.0, "confidence": "high", "reasons": ["advanced variant"] }
      ],
      "regressions": [
        { "id": "0748", "name": "smith bench press", "types": ["lower_difficulty"], "score": 100.0, "confidence": "high", "reasons": ["beginner variant"] }
      ]
    }
    """;
}
