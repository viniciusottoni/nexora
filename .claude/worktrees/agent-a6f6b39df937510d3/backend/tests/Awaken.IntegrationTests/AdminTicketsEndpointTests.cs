using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Awaken.Contracts.Admin.Tickets;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Support;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

/// <summary>
/// US-162 / US-163: testes de integração para acompanhamento e triagem de tickets de suporte
/// no site admin. Usa PostgreSQL real via Testcontainers.
///
/// O token AdminBearer é gerado diretamente no teste (mesmo segredo/issuer/audience do AdminJwt
/// em appsettings.Development.json), desacoplando este teste do módulo de autenticação admin.
/// </summary>
public class AdminTicketsEndpointTests : IAsyncLifetime
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

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string GenerateAdminBearerToken(Guid? adminId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var section = config.GetSection("AdminJwt");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, (adminId ?? Guid.NewGuid()).ToString()),
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

    private async Task<(string token, Guid userId)> RegisterAndGetTokenAsync(string email)
    {
        var payload = new { email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        return (auth.AccessToken, auth.User.Id);
    }

    private async Task<Guid> SeedSupportTicketAsync(string userToken, string category = "report", string description = "O app travou durante o treino.")
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var request = new CreateSupportTicketRequest(category, description, "1.0.0", null);
        var response = await _client.PostAsJsonAsync("/api/v1/support/tickets", request);
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<SupportTicketResponse>())!;
        _client.DefaultRequestHeaders.Authorization = null;
        return result.Id;
    }

    // ─── Autenticação ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTickets_WhenUnauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/admin/tickets");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTickets_WithValidAdminBearerToken_Returns200()
    {
        var adminToken = GenerateAdminBearerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.GetAsync("/api/admin/tickets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── Fluxo completo: criar (app) → triar (admin) → consultar histórico ──

    [Fact]
    public async Task TriageTicket_PersistsStatusAndIsRetrievableInHistory()
    {
        var (userToken, _) = await RegisterAndGetTokenAsync($"ticket-user-{Guid.NewGuid():N}@awaken.app");
        var ticketId = await SeedSupportTicketAsync(userToken);

        var adminId = Guid.NewGuid();
        var adminToken = GenerateAdminBearerToken(adminId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var triageRequest = new TriageTicketRequest("in_triagem", "high", null);
        var triageResponse = await _client.PutAsJsonAsync($"/api/admin/tickets/{ticketId}/triage", triageRequest);
        triageResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detailResponse = await _client.GetAsync($"/api/admin/tickets/{ticketId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await detailResponse.Content.ReadFromJsonAsync<AdminTicketDetailResponse>();
        detail.Should().NotBeNull();
        detail!.Status.Should().Be("in_triagem");
        detail.Priority.Should().Be("high");
        detail.History.Should().HaveCount(2,
            "deve haver um evento de status_change e um de priority_change");
        detail.History.Should().Contain(e => e.EventType == "status_change" && e.NewValue == "in_triagem");
        detail.History.Should().Contain(e => e.EventType == "priority_change" && e.NewValue == "high");
        detail.History.Should().OnlyContain(e => e.AdminId == adminId);
    }

    [Fact]
    public async Task GetTickets_ListsSeededTicketWithTruncatedDescription()
    {
        var (userToken, userId) = await RegisterAndGetTokenAsync($"ticket-list-{Guid.NewGuid():N}@awaken.app");
        await SeedSupportTicketAsync(userToken, "question", "Como funciona o sistema de XP no Awaken?");

        var adminToken = GenerateAdminBearerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.GetAsync($"/api/admin/tickets?category=question&page=1&pageSize=20");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await response.Content.ReadFromJsonAsync<AdminTicketListResponse>();
        list.Should().NotBeNull();
        list!.Items.Should().Contain(t => t.UserId == userId && t.Category == "question");
    }

    [Fact]
    public async Task AddTicketNote_PersistsNoteInHistory()
    {
        var (userToken, _) = await RegisterAndGetTokenAsync($"ticket-note-{Guid.NewGuid():N}@awaken.app");
        var ticketId = await SeedSupportTicketAsync(userToken);

        var adminId = Guid.NewGuid();
        var adminToken = GenerateAdminBearerToken(adminId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var noteRequest = new AddTicketNoteRequest("Aguardando retorno do usuário.");
        var noteResponse = await _client.PostAsJsonAsync($"/api/admin/tickets/{ticketId}/notes", noteRequest);
        noteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detailResponse = await _client.GetAsync($"/api/admin/tickets/{ticketId}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<AdminTicketDetailResponse>();

        detail!.History.Should().ContainSingle(e =>
            e.EventType == "internal_note" &&
            e.NoteContent == "Aguardando retorno do usuário." &&
            e.AdminId == adminId);
    }

    [Fact]
    public async Task TriageTicket_WithInvalidStatus_Returns422()
    {
        var (userToken, _) = await RegisterAndGetTokenAsync($"ticket-invalid-{Guid.NewGuid():N}@awaken.app");
        var ticketId = await SeedSupportTicketAsync(userToken);

        var adminToken = GenerateAdminBearerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var triageRequest = new TriageTicketRequest("invalid_status", null, null);
        var response = await _client.PutAsJsonAsync($"/api/admin/tickets/{ticketId}/triage", triageRequest);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task TriageTicket_WhenTicketDoesNotExist_Returns404()
    {
        var adminToken = GenerateAdminBearerToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var triageRequest = new TriageTicketRequest("in_triagem", null, null);
        var response = await _client.PutAsJsonAsync($"/api/admin/tickets/{Guid.NewGuid()}/triage", triageRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
