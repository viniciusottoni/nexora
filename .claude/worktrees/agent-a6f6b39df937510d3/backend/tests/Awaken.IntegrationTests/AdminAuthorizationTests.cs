using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Exercises;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class AdminAuthorizationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("awaken")
        .WithUsername("awaken")
        .WithPassword("awaken_test_password")
        .Build();

    private readonly string _exerciseDir = Path.Combine(
        Path.GetTempPath(),
        $"awaken-admin-auth-it-{Guid.NewGuid():N}");

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_exerciseDir);
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                // ExerciseImport:RootDirectory precisa ser sobrescrito aqui (não via
                // builder.UseSetting) - appsettings.json define "" explicitamente, e essa
                // fonte de config é carregada DEPOIS dos webHost settings, então UseSetting
                // é silenciosamente sobrescrito de volta para "" (SafeDirectoryResolver.Resolve
                // sempre retorna null, e o import cai em ValidationException/422 em vez de 200
                // no teste AdminEndpointReturns200ForAdminUser). AddInMemoryCollection via
                // ConfigureAppConfiguration é adicionado por último e vence, igual ao
                // ConnectionStrings:PostgreSQL logo abaixo.
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PostgreSQL"] = _postgres.GetConnectionString(),
                    ["ExerciseImport:RootDirectory"] = Path.GetTempPath(),
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

        if (Directory.Exists(_exerciseDir))
            Directory.Delete(_exerciseDir, recursive: true);
    }

    private async Task<string> RegisterAndLoginAsync(string email)
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Str0ngPass!",
            name = "Hunter",
            language = "pt-BR"
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Str0ngPass!" });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.AccessToken;
    }

    private async Task<string> RegisterAdminAndLoginAsync(string email)
    {
        // Register first
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Str0ngPass!",
            name = "Admin",
            language = "pt-BR"
        });

        // Set role directly in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE users SET \"Role\" = 'Admin' WHERE \"Id\" = {0}", user.Id);

        // Login to get a token that carries the Admin role
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Str0ngPass!" });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.AccessToken;
    }

    [Fact]
    public async Task AdminEndpointReturns403ForNonAdminAuthenticatedUser()
    {
        var token = await RegisterAndLoginAsync($"regular-{Guid.NewGuid():N}@awaken.app");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/exercises/import")
        {
            Content = JsonContent.Create(new ImportExercisesRequest(_exerciseDir, "local_files"))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpointReturns401ForUnauthenticatedRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/exercises/import")
        {
            Content = JsonContent.Create(new ImportExercisesRequest(_exerciseDir, "local_files"))
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpointReturns200ForAdminUser()
    {
        var token = await RegisterAdminAndLoginAsync($"admin-{Guid.NewGuid():N}@awaken.app");

        // SafeDirectoryResolver rejeita batchKey absoluto por design (RN de path traversal) — usa
        // o nome relativo da pasta dentro de ExerciseImport:RootDirectory (Path.GetTempPath()).
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/exercises/import")
        {
            Content = JsonContent.Create(
                new ImportExercisesRequest(Path.GetFileName(_exerciseDir), "local_files"))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        // The directory is empty so 0 imported, but admin access should be granted (200 OK)
        response.StatusCode.Should().Be(HttpStatusCode.OK, "response body was: {0}", body);
    }
}
