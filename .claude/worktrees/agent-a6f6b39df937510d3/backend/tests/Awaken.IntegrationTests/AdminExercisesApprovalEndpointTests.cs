using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Exercises;
using Awaken.Domain.Entities.Exercises;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

/// <summary>
/// US-149 (R3.3) — <c>POST /api/admin/exercises/{id}/approve</c> e <c>/reject</c>. Reaproveita o
/// mesmo padrão de <see cref="FakeMediaStorageService"/> e de elevação de role (definidos em
/// <see cref="ExerciseImportEndpointTests"/>) para não depender de credenciais reais do bucket S3-compatível.
/// </summary>
public class AdminExercisesApprovalEndpointTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("awaken")
        .WithUsername("awaken")
        .WithPassword("awaken_test_password")
        .Build();

    private readonly string _importRootDirectory = Path.Combine(
        Path.GetTempPath(),
        $"awaken-exercises-approval-root-{Guid.NewGuid():N}");

    private const string BatchKey = "batch-2026-01";
    private string BatchDirectory => Path.Combine(_importRootDirectory, BatchKey);

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(BatchDirectory);
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PostgreSQL"] = _postgres.GetConnectionString(),
                    ["ExerciseImport:RootDirectory"] = _importRootDirectory,
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

    private async Task<string> RegisterAndLoginAsAdminAsync()
    {
        var email = $"admin-approval-{Guid.NewGuid():N}@awaken.app";
        const string password = "Str0ngPass!";

        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            name = "Admin",
            language = "pt-BR"
        });

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

    private async Task<Guid> ImportOneApprovableExerciseAsync(string accessToken)
    {
        File.WriteAllText(Path.Combine(BatchDirectory, "0025.json"), SampleExerciseJson("0025"));
        File.WriteAllBytes(Path.Combine(BatchDirectory, "0025-360.gif"), [71, 73, 70, 56]);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/exercises/import")
        {
            Content = JsonContent.Create(new ImportExercisesRequest(BatchKey, "local_files")),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        await _client.SendAsync(request);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var catalog = await db.ExerciseCatalogs.FirstAsync(e => e.ProviderExerciseId == "0025");
        return catalog.Id;
    }

    [Fact]
    public async Task ApproveReturnsOkAndApprovesExerciseWhenAdminAndItMeetsAllCriteria()
    {
        var accessToken = await RegisterAndLoginAsAdminAsync();
        var exerciseId = await ImportOneApprovableExerciseAsync(accessToken);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/exercises/{exerciseId}/approve");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApproveExerciseResponse>();
        result!.Status.Should().Be("approved");
        result.IsApprovedForWorkoutGeneration.Should().BeTrue();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var saved = await db.ExerciseCatalogs.FirstAsync(e => e.Id == exerciseId);
        saved.IsApprovedForWorkoutGeneration.Should().BeTrue();
        saved.SanitizationStatus.Should().Be("approved");
    }

    [Fact]
    public async Task RejectReturnsOkAndRecordsReasonWhenAdmin()
    {
        var accessToken = await RegisterAndLoginAsAdminAsync();
        var exerciseId = await ImportOneApprovableExerciseAsync(accessToken);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/exercises/{exerciseId}/reject")
        {
            Content = JsonContent.Create(new RejectExerciseRequest("nao atende ao padrao de qualidade")),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RejectExerciseResponse>();
        result!.Status.Should().Be("rejected");
        result.RejectionReason.Should().Be("nao atende ao padrao de qualidade");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var saved = await db.ExerciseCatalogs.FirstAsync(e => e.Id == exerciseId);
        saved.IsApprovedForWorkoutGeneration.Should().BeFalse();
        saved.SanitizationStatus.Should().Be("rejected");
    }

    [Fact]
    public async Task ApproveReturnsUnauthorizedWithoutToken()
    {
        var response = await _client.PostAsync(
            $"/api/admin/exercises/{Guid.NewGuid()}/approve", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApproveReturnsNotFoundWhenExerciseDoesNotExist()
    {
        var accessToken = await RegisterAndLoginAsAdminAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/exercises/{Guid.NewGuid()}/approve");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
      }
    }
    """;
}
