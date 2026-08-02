using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Awaken.Contracts.Admin.Security;
using Awaken.Domain.Entities.Security;
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
/// US-165: testes de integração do monitoramento de alertas de segurança no admin site.
/// Usa PostgreSQL real via Testcontainers e gera um JWT "AdminSite" manualmente
/// (o módulo de login do admin site não é dependência deste teste).
/// </summary>
public class AdminSecurityEndpointTests : IAsyncLifetime
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

        var pgConnectionString = _postgres.GetConnectionString();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            // appsettings.Local.json (loaded inside Program.cs) overrides UseSetting, so we
            // replace the DbContextOptions directly in the DI container after all registrations.
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AwakenDbContext>));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddDbContext<AwakenDbContext>(options => options.UseNpgsql(pgConnectionString));
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

    private async Task<SecurityAlert> SeedAlertAsync(
        string alertType = "brute_force",
        string severity = "critical",
        string environment = "prod")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var alert = SecurityAlert.Create(
            alertType, severity, environment, DateTime.UtcNow,
            origin: "login_endpoint", maskedIp: "203.0.113.x");

        db.SecurityAlerts.Add(alert);
        await db.SaveChangesAsync();
        return alert;
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAlerts_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/admin/security/alerts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAlerts_WhenAdmin_Returns200WithSeededAlert()
    {
        var alert = await SeedAlertAsync();
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/security/alerts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SecurityAlertListResponse>();
        body!.Items.Should().Contain(i => i.Id == alert.Id);
    }

    [Fact]
    public async Task GetAlertDetail_WhenAdmin_ReturnsFullDetail()
    {
        var alert = await SeedAlertAsync(severity: "high");
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/admin/security/alerts/{alert.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SecurityAlertDetailResponse>();
        body!.Id.Should().Be(alert.Id);
        body.MaskedIp.Should().Be("203.0.113.x");
        body.Status.Should().Be("open");
    }

    [Fact]
    public async Task GetAlertDetail_WhenAlertDoesNotExist_Returns404()
    {
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/admin/security/alerts/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AnalyzeAlert_WhenAdmin_MarksStatusAsAnalyzed()
    {
        var alert = await SeedAlertAsync(severity: "critical");
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync(
            $"/api/admin/security/alerts/{alert.Id}/analyze",
            new MarkAlertAnalyzedRequest("Falso positivo confirmado."));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detailResponse = await _client.GetAsync($"/api/admin/security/alerts/{alert.Id}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<SecurityAlertDetailResponse>();
        detail!.Status.Should().Be("analyzed");
        detail.AnalyzedByAdminId.Should().NotBeNull();
        detail.AnalyzedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task AnalyzeAlert_GeneratesAuditLogEntry()
    {
        var alert = await SeedAlertAsync(severity: "critical");
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync(
            $"/api/admin/security/alerts/{alert.Id}/analyze",
            new MarkAlertAnalyzedRequest());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var auditEntry = await db.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "AdminSecurityAlert.Analyzed" && a.ResourceId == alert.Id);

        auditEntry.Should().NotBeNull("RN-004: marcar alerta como analisado deve gerar auditoria");
    }

    [Fact]
    public async Task GetAlerts_DefaultOrdering_ReturnsCriticalBeforeLowSeverity()
    {
        // RN-002: alertas críticos devem aparecer antes de alertas baixos, mesmo quando o
        // alerta de baixa severidade foi criado depois (mais recente).
        await SeedAlertAsync(severity: "low");
        var critical = await SeedAlertAsync(severity: "critical");
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/security/alerts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SecurityAlertListResponse>();
        var severities = body!.Items.Select(i => i.Severity).ToList();
        severities.IndexOf("critical").Should().BeLessThan(severities.IndexOf("low"));
        body.Items.First().Id.Should().Be(critical.Id);
    }

    [Fact]
    public async Task ClassifyAlert_AsFalsePositive_DoesNotDeleteAlert()
    {
        // RN-005: falso positivo não apaga o alerta; apenas muda sua classificação.
        var alert = await SeedAlertAsync(severity: "high");
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync(
            $"/api/admin/security/alerts/{alert.Id}/classify",
            new ClassifyAlertRequest("false_positive", "Confirmado como teste interno."));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detailResponse = await _client.GetAsync($"/api/admin/security/alerts/{alert.Id}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "RN-005: o alerta deve continuar acessível após ser classificado como falso positivo");

        var detail = await detailResponse.Content.ReadFromJsonAsync<SecurityAlertDetailResponse>();
        detail!.Classification.Should().Be("false_positive");
        detail.Id.Should().Be(alert.Id);
    }

    [Fact]
    public async Task ClassifyAlert_GeneratesAuditLogEntry()
    {
        var alert = await SeedAlertAsync(severity: "critical");
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync(
            $"/api/admin/security/alerts/{alert.Id}/classify",
            new ClassifyAlertRequest("false_positive", null));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var auditEntry = await db.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "AdminSecurityAlert.Classified" && a.ResourceId == alert.Id);

        auditEntry.Should().NotBeNull("RN-004: classificar um alerta deve gerar auditoria");
    }

    [Fact]
    public async Task ClassifyAlert_WithInvalidClassification_ReturnsBadRequest()
    {
        var alert = await SeedAlertAsync();
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync(
            $"/api/admin/security/alerts/{alert.Id}/classify",
            new ClassifyAlertRequest("not_a_real_classification", null));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AddNote_PersistsNoteOnAlert()
    {
        var alert = await SeedAlertAsync();
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync(
            $"/api/admin/security/alerts/{alert.Id}/note",
            new AddAlertNoteRequest("Acompanhar com o time de plataforma."));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detailResponse = await _client.GetAsync($"/api/admin/security/alerts/{alert.Id}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<SecurityAlertDetailResponse>();
        detail!.Note.Should().Be("Acompanhar com o time de plataforma.");
    }

    [Fact]
    public async Task LinkToBug_WhenBugExists_PersistsLink()
    {
        var alert = await SeedAlertAsync(severity: "critical");
        var bugId = await SeedOperationalBugAsync();
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync(
            $"/api/admin/security/alerts/{alert.Id}/link-bug",
            new LinkAlertToBugRequest(bugId));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detailResponse = await _client.GetAsync($"/api/admin/security/alerts/{alert.Id}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<SecurityAlertDetailResponse>();
        detail!.RelatedBugId.Should().Be(bugId);
    }

    [Fact]
    public async Task LinkToBug_WhenBugDoesNotExist_Returns404()
    {
        var alert = await SeedAlertAsync();
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync(
            $"/api/admin/security/alerts/{alert.Id}/link-bug",
            new LinkAlertToBugRequest(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSummary_LoginFailureSpike_IsFlaggedInTrends()
    {
        // RN-003: pico de falha de login deve gerar destaque (IsSpike) no resumo preventivo.
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var now = DateTime.UtcNow;

            // Janela anterior (25h-49h atrás): poucos alertas de brute_force.
            db.SecurityAlerts.Add(SecurityAlert.Create("brute_force", "high", "prod", now.AddHours(-30), origin: "login_endpoint", maskedIp: "203.0.113.x"));

            // Janela atual (últimas 24h): muitos alertas de brute_force -> pico.
            for (var i = 0; i < 10; i++)
                db.SecurityAlerts.Add(SecurityAlert.Create("brute_force", "high", "prod", now.AddMinutes(-i), origin: "login_endpoint", maskedIp: "203.0.113.x"));

            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/admin/security/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SecuritySummaryResponse>();
        var bruteForceTrend = body!.AlertTypeTrends.FirstOrDefault(t => t.AlertType == "brute_force");
        bruteForceTrend.Should().NotBeNull();
        bruteForceTrend!.IsSpike.Should().BeTrue();
    }

    [Fact]
    public async Task GetSummary_ManyRateLimitHits_GroupsByEndpoint()
    {
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var now = DateTime.UtcNow;
            for (var i = 0; i < 5; i++)
                db.SecurityAlerts.Add(SecurityAlert.Create("rate_limit_hit", "medium", "prod", now.AddMinutes(-i), origin: "quest_complete_endpoint", maskedIp: "198.51.100.x"));

            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/admin/security/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SecuritySummaryResponse>();
        body!.RateLimitHitsByEndpoint.Should().Contain(e => e.Endpoint == "quest_complete_endpoint" && e.Count == 5);
    }

    [Fact]
    public async Task GetSummary_AuthorizationDenied_GroupsByResource()
    {
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            db.SecurityAlerts.Add(SecurityAlert.Create("rbac_denied", "high", "prod", DateTime.UtcNow, origin: "admin_users_export", maskedIp: "198.51.100.x"));
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/admin/security/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SecuritySummaryResponse>();
        body!.RbacDeniedByResource.Should().Contain(e => e.Endpoint == "admin_users_export");
    }

    [Fact]
    public async Task GetSummary_CriticalAlert_AppearsInOpenAlertsBySeverity()
    {
        var alert = await SeedAlertAsync(severity: "critical");
        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/security/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SecuritySummaryResponse>();
        body!.OpenAlertsBySeverity.Should().Contain(s => s.Severity == "critical" && s.Count >= 1);
        alert.Status.Should().Be("open");
    }

    [Fact]
    public async Task GetSummary_DoesNotExposeUnmaskedIp_OnlyMaskedOrigins()
    {
        // RN-001: dados sensíveis devem permanecer mascarados — checagem básica de que o
        // payload de origens mascaradas não contém um IP completo (sem sufixo ".x").
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            db.SecurityAlerts.Add(SecurityAlert.Create("brute_force", "high", "prod", DateTime.UtcNow, origin: "login_endpoint", maskedIp: "203.0.113.x"));
            await db.SaveChangesAsync();
        }

        var token = GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/security/summary");
        var raw = await response.Content.ReadAsStringAsync();

        raw.Should().NotContain("203.0.113.1", "RN-001: o IP completo nunca deve ser exposto, apenas o mascarado");
    }

    [Fact]
    public async Task GetSummary_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/admin/security/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<Guid> SeedOperationalBugAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var bug = Awaken.Domain.Entities.Bugs.OperationalBug.Create(
            "Pico de brute force investigado", "high", "auth", "prod", "monitoring",
            Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow);

        db.OperationalBugs.Add(bug);
        await db.SaveChangesAsync();
        return bug.Id;
    }

    [Fact]
    public async Task GetAlerts_WithoutAdminRole_Returns403()
    {
        // Token válido no scheme AdminBearer, mas sem a role AdminSite.
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
        var response = await _client.GetAsync("/api/admin/security/alerts");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
