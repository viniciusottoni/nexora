using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class AuthForgotPasswordEndpointTests : IAsyncLifetime
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

    private async Task RegisterAsync(string email, string password = "Str0ngPass!")
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            name = "Hunter",
            language = "pt-BR"
        });
    }

    [Fact]
    public async Task ForgotPasswordReturnsOkWithGenericSuccessForExistingEmail()
    {
        await RegisterAsync("hunter@awaken.app");
        _client.DefaultRequestHeaders.Add("X-Correlation-Id", "fp-corr-1");

        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email = "hunter@awaken.app" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("message").GetString()
            .Should().Be("Se existir uma conta com este e-mail, enviaremos instruções de recuperação.");
        document.RootElement.GetProperty("correlationId").GetString().Should().Be("fp-corr-1");
    }

    [Fact]
    public async Task ForgotPasswordReturnsOkWithGenericSuccessForNonExistingEmail()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email = "nobody@awaken.app" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("message").GetString()
            .Should().Be("Se existir uma conta com este e-mail, enviaremos instruções de recuperação.");
    }

    [Fact]
    public async Task ForgotPasswordReturnsValidationErrorForInvalidEmail()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email = "not-an-email" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ForgotPasswordReturnsValidationErrorForEmptyEmail()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email = "" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ForgotPasswordDoesNotRevealWhetherEmailExistsInResponse()
    {
        await RegisterAsync("exists@awaken.app");

        var responseExists = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email = "exists@awaken.app" });
        var responseNotExists = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email = "notexists@awaken.app" });

        responseExists.StatusCode.Should().Be(HttpStatusCode.OK);
        responseNotExists.StatusCode.Should().Be(HttpStatusCode.OK);

        using var docExists = JsonDocument.Parse(await responseExists.Content.ReadAsStringAsync());
        using var docNotExists = JsonDocument.Parse(await responseNotExists.Content.ReadAsStringAsync());

        docExists.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        docNotExists.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        docExists.RootElement.GetProperty("message").GetString()
            .Should().Be(docNotExists.RootElement.GetProperty("message").GetString());
    }
}
