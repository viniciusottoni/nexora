using System.IdentityModel.Tokens.Jwt;
using Nexora.Application.Auth.Commands.LoginWithPin;
using Nexora.Application.Auth.Shared;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Auth;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection("Postgres")]
public sealed class LoginWithPinIntegrationTests
{
    private const string TestPepper = "integration-test-secret-pepper-nao-e-producao";
    private readonly PostgresFixture _fixture;

    public LoginWithPinIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_Por_Pin_Persiste_O_Mesmo_Id_De_Sessao_Emitido_No_Token()
    {
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        const string pin = "2101";
        const string deviceSecret = "segredo-do-dispositivo";

        await using (var rootDb = _fixture.CreateAppDbContext(tenantContext: null))
        {
            rootDb.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await rootDb.SaveChangesAsync();
        }

        Guid deviceId;
        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId)))
        {
            seedDb.Stores.Add(Store.Create(storeId, tenantId, "Matriz", isDefault: true));

            var role = Role.Create(tenantId, "OWNER", "Proprietário", isSystem: true);
            role.UpdatePermissions("[\"*\"]");
            seedDb.Roles.Add(role);

            var authOptions = Options.Create(new AuthSecretsOptions
            {
                SecretPepper = TestPepper,
                PinLookupPepper = TestPepper,
            });
            var credentialHasher = new Argon2CredentialHasher();
            var pinLookup = new HmacPinLookupDigester(authOptions);
            var user = AppUser.Create(
                tenantId,
                "Gestora local",
                email: null,
                passwordHash: null,
                pinHash: credentialHasher.Hash(pin),
                pinLookup: pinLookup.Digest(pin));
            seedDb.Users.Add(user);
            seedDb.UserRoles.Add(UserRole.Create(tenantId, user.Id, role.Id, storeId));

            var secretDigester = new HmacSecretDigester(authOptions);
            var device = Device.Create(tenantId, storeId, "Gestão local", DeviceType.Tablet, "fingerprint-admin");
            device.SetSecret(secretDigester.Digest(deviceSecret));
            seedDb.Devices.Add(device);
            deviceId = device.Id;

            await seedDb.SaveChangesAsync();
        }

        await using var loginDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId));
        using var provider = MediatRTestContainerFactory.Build(loginDb, new StaticTenantContext(tenantId, storeId));
        var result = await provider.GetRequiredService<IMediator>()
            .Send(new LoginWithPinCommand(deviceId, pin, deviceSecret));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        var tokenSessionId = Guid.Parse(
            new JwtSecurityTokenHandler()
                .ReadJwtToken(result.Value!.AccessToken)
                .Claims.Single(claim => claim.Type == "ses")
                .Value);

        var persistedSession = await loginDb.AuthSessions.SingleAsync(session => session.DeviceId == deviceId);
        persistedSession.Id.Should().Be(tokenSessionId);

        var guardResult = await new AuthSessionActivityGuard(loginDb)
            .EnforceAsync(tenantId, tokenSessionId);
        guardResult.IsSuccess.Should().BeTrue("a primeira chamada após o login não pode expirar a sessão");
    }
}
