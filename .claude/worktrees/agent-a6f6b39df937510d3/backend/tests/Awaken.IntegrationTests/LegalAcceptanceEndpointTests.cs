using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Legal;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class LegalAcceptanceEndpointTests : IAsyncLifetime
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

    private async Task<string> RegisterAndLoginAsync(
        string email = "hunter@awaken.app",
        string password = "Str0ngPass!",
        string name = "Hunter")
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            name,
            language = "pt-BR"
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.AccessToken;
    }

    [Fact]
    public async Task AcceptLegalTermsReturnsOkAndPersistsAcceptance()
    {
        var token = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/users/me/legal-acceptance", new
        {
            termsVersion = "1.0.0",
            privacyVersion = "1.0.0",
            accepted = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LegalStatusResponse>();
        body!.HasAcceptedLegal.Should().BeTrue();
        body.TermsVersion.Should().Be("1.0.0");
        body.PrivacyVersion.Should().Be("1.0.0");
        body.TermsAcceptedAt.Should().NotBeNull();
        body.PrivacyAcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AcceptLegalTermsReturnsUnprocessableEntityWhenAcceptedIsFalse()
    {
        var token = await RegisterAndLoginAsync("hunter2@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/users/me/legal-acceptance", new
        {
            termsVersion = "1.0.0",
            privacyVersion = "1.0.0",
            accepted = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AcceptLegalTermsReturnsUnauthorizedWhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/users/me/legal-acceptance", new
        {
            termsVersion = "1.0.0",
            privacyVersion = "1.0.0",
            accepted = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AcceptLegalTermsReturnsUnprocessableEntityWhenVersionIsMissing()
    {
        var token = await RegisterAndLoginAsync("hunter3@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/users/me/legal-acceptance", new
        {
            accepted = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AcceptResponsibilityNoticeReturnsOkAndPersistsAcceptance()
    {
        var token = await RegisterAndLoginAsync("hunter4@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/users/me/responsibility-notice", new
        {
            noticeVersion = "1.0.0",
            accepted = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LegalStatusResponse>();
        body!.HasAcceptedResponsibilityNotice.Should().BeTrue();
        body.ResponsibilityNoticeVersion.Should().Be("1.0.0");
        body.ResponsibilityNoticeAcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AcceptResponsibilityNoticeReturnsUnprocessableEntityWhenAcceptedIsFalse()
    {
        var token = await RegisterAndLoginAsync("hunter5@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/users/me/responsibility-notice", new
        {
            noticeVersion = "1.0.0",
            accepted = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AcceptResponsibilityNoticeReturnsUnauthorizedWhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/users/me/responsibility-notice", new
        {
            noticeVersion = "1.0.0",
            accepted = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLegalStatusReturnsCurrentAcceptanceState()
    {
        var token = await RegisterAndLoginAsync("hunter6@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var statusBefore = await _client.GetAsync("/api/users/me/legal-status");
        statusBefore.StatusCode.Should().Be(HttpStatusCode.OK);
        var bodyBefore = await statusBefore.Content.ReadFromJsonAsync<LegalStatusResponse>();
        bodyBefore!.HasAcceptedLegal.Should().BeFalse();
        bodyBefore.HasAcceptedResponsibilityNotice.Should().BeFalse();

        await _client.PostAsJsonAsync("/api/users/me/legal-acceptance", new
        {
            termsVersion = "1.0.0",
            privacyVersion = "1.0.0",
            accepted = true
        });

        var statusAfter = await _client.GetAsync("/api/users/me/legal-status");
        statusAfter.StatusCode.Should().Be(HttpStatusCode.OK);
        var bodyAfter = await statusAfter.Content.ReadFromJsonAsync<LegalStatusResponse>();
        bodyAfter!.HasAcceptedLegal.Should().BeTrue();
    }
}
