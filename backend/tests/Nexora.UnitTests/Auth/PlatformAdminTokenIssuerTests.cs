using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nexora.Application.Abstractions.Security;
using Nexora.Infrastructure.Auth;
using Xunit;

namespace Nexora.UnitTests.Auth;

public sealed class PlatformAdminTokenIssuerTests
{
    private readonly JwtTokenIssuer _issuer = new(Options.Create(new JwtOptions
    {
        Secret = "unit-tests-platform-admin-secret-with-32-bytes",
        Issuer = "nexora-tests",
        Audience = "nexora-tests",
    }));

    [Fact]
    public async Task PlatformAdmin_Recebe_Claim_Dedicada_De_Plataforma()
    {
        var token = await _issuer.IssueAccessTokenAsync(Claims("PLATFORM_ADMIN"), 300);

        var payload = new JwtSecurityTokenHandler().ReadJwtToken(token).Payload;

        payload["plt"].Should().Be("admin");
    }

    [Fact]
    public async Task Papel_De_Tenant_Nao_Recebe_Claim_De_Plataforma()
    {
        var token = await _issuer.IssueAccessTokenAsync(Claims("OWNER"), 300);

        var payload = new JwtSecurityTokenHandler().ReadJwtToken(token).Payload;

        payload.Should().NotContainKey("plt");
    }

    private static AccessClaims Claims(string role) => new(
        Subject: Guid.NewGuid(),
        TenantId: Guid.NewGuid(),
        StoreId: Guid.NewGuid(),
        Roles: new[] { role },
        Permissions: new[] { "*" });
}
