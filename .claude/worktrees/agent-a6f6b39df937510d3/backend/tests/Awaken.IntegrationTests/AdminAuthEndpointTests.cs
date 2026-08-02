using System.Net;
using System.Net.Http.Json;
using Awaken.Contracts.Admin.Auth;
using Awaken.Domain.Entities.Admin;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

public class AdminAuthEndpointTests : IAsyncLifetime
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

    private async Task<AdminUser> SeedAdminUserAsync(string email, string passwordHash)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        var adminUser = AdminUser.Create(email, passwordHash, DateTime.UtcNow);
        db.AdminUsers.Add(adminUser);
        await db.SaveChangesAsync();

        return adminUser;
    }

    [Fact]
    public async Task LoginReturnsGenericFailureWhenNoAdminUserExists()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/auth/login", new AdminLoginRequest(
            $"missing-{Guid.NewGuid():N}@awaken.app", "Str0ngPass!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullLoginAndMfaSetupAndVerifyFlowIssuesAccessToken()
    {
        // BCrypt hash for "Str0ngPass!1" computed via the app's own hasher would require DI;
        // instead, use the API's own hasher indirectly by seeding via the repository through DI.
        using var scope = _factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<Awaken.Application.Common.Interfaces.IPasswordHasher>();
        var passwordHash = hasher.Hash("Str0ngPass!1");

        var email = $"admin-{Guid.NewGuid():N}@awaken.app";
        var adminUser = await SeedAdminUserAsync(email, passwordHash);

        // Step 1: login (no MFA enabled yet) -> RequiresMfaSetup
        var loginResponse = await _client.PostAsJsonAsync("/api/admin/auth/login",
            new AdminLoginRequest(email, "Str0ngPass!1"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AdminLoginResponse>();
        loginResult!.RequiresMfaSetup.Should().BeTrue();
        loginResult.RequiresMfaChallenge.Should().BeFalse();
        loginResult.AccessToken.Should().BeNull();
        loginResult.AdminUserId.Should().Be(adminUser.Id);

        // Step 2: mfa/setup -> QR + secret
        var setupResponse = await _client.PostAsJsonAsync("/api/admin/auth/mfa/setup",
            new AdminMfaSetupRequest(adminUser.Id));
        setupResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var setupResult = await setupResponse.Content.ReadFromJsonAsync<AdminMfaSetupResponse>();
        setupResult!.SecretKey.Should().NotBeNullOrWhiteSpace();
        setupResult.QrCodeBase64.Should().NotBeNullOrWhiteSpace();

        // Step 3: compute a real TOTP code from the returned secret and verify
        var totp = new Totp(Base32Encoding.ToBytes(setupResult.SecretKey));
        var code = totp.ComputeTotp();

        var verifyResponse = await _client.PostAsJsonAsync("/api/admin/auth/mfa/verify",
            new AdminMfaVerifyRequest(adminUser.Id, code));
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var verifyResult = await verifyResponse.Content.ReadFromJsonAsync<AdminMfaVerifyResponse>();
        verifyResult!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }
}
