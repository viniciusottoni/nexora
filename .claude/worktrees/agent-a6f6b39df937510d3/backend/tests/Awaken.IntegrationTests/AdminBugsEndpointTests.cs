using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Awaken.Contracts.Admin.Bugs;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

/// <summary>
/// US-164 / US-171: testes de integração dos endpoints administrativos de bugs operacionais.
/// Gera o token AdminBearer diretamente no teste (config "AdminJwt" + role "AdminSite"),
/// sem depender do módulo de autenticação admin estar concluído.
/// </summary>
public class AdminBugsEndpointTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("awaken")
        .WithUsername("awaken")
        .WithPassword("awaken_test_password")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    private const string AdminJwtSecret = "awaken-admin-dev-secret-key-at-least-32-chars!!";
    private const string AdminJwtIssuer = "awaken-api";
    private const string AdminJwtAudience = "awaken-admin";

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

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BuildAdminBearerToken());
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private static string BuildAdminBearerToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AdminJwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "AdminSite"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: AdminJwtIssuer,
            audience: AdminJwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static CreateOperationalBugRequest ValidCreateRequest(string title = "Crash ao abrir perfil") => new(
        title,
        "high",
        "profile-service",
        "prod",
        "internal",
        DateTime.UtcNow.AddMinutes(-30),
        "Stack trace anexado em log interno.",
        "corr-abc-123",
        null,
        null);

    [Fact]
    public async Task PostCreatesBugAndItAppearsInGetList()
    {
        var request = ValidCreateRequest();

        var createResponse = await _client.PostAsJsonAsync("/api/admin/bugs", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await _client.GetAsync("/api/admin/bugs?page=1&pageSize=20");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await listResponse.Content.ReadFromJsonAsync<AdminBugListResponse>();
        list.Should().NotBeNull();
        list!.Items.Should().Contain(b => b.Title == request.Title);
    }

    [Fact]
    public async Task PutUpdatesStatusAndHistoryShowsChangeViaGetDetail()
    {
        var request = ValidCreateRequest("Erro ao salvar treino");
        var createResponse = await _client.PostAsJsonAsync("/api/admin/bugs", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CreatedBugIdResponse>();
        created.Should().NotBeNull();

        var updateRequest = new UpdateOperationalBugRequest("in_progress", null, "Investigando causa raiz.");
        var updateResponse = await _client.PutAsJsonAsync($"/api/admin/bugs/{created!.Id}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detailResponse = await _client.GetAsync($"/api/admin/bugs/{created.Id}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await detailResponse.Content.ReadFromJsonAsync<AdminBugDetailResponse>();
        detail.Should().NotBeNull();
        detail!.Status.Should().Be("in_progress");
        detail.History.Should().Contain(e => e.EventType == "status_change" && e.NewValue == "in_progress");
        detail.History.Should().Contain(e => e.EventType == "comment" && e.Comment == "Investigando causa raiz.");
    }

    [Fact]
    public async Task PostMissingRequiredFieldBlocksCreationWithClearError()
    {
        // RN-001 / CA-002: campo obrigatório ausente bloqueia a criação com erro claro.
        // O pipeline de validação deste backend mapeia falhas de FluentValidation para
        // 422 Unprocessable Entity (ver ExceptionHandlingMiddleware), não 400.
        var invalidRequest = new CreateOperationalBugRequest(
            "Título válido",
            "", // severity ausente
            "component-x",
            "prod",
            "internal",
            DateTime.UtcNow,
            null,
            null,
            null,
            null);

        var response = await _client.PostAsJsonAsync("/api/admin/bugs", invalidRequest);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private record CreatedBugIdResponse(Guid Id);
}
