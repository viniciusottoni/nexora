using Nexora.Application.Abstractions.Security;
using Nexora.Application.Auth.Shared;
using Nexora.Infrastructure.Auth;
using Nexora.Shared.Errors;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Nexora.UnitTests.Auth;

/// <summary>
/// US-004, gap "X-Authorization-Token (autorização pontual) é só EMITIDO, nunca VALIDADO" — cobre
/// <see cref="AuthorizationTokenValidator"/> (a peça que faltava) contra o <see cref="JwtTokenIssuer"/>
/// REAL de Infrastructure (mesmo emissor usado por <c>AuthorizeSensitiveActionCommandHandler</c> em
/// produção), sem precisar de um endpoint de negócio real para exercitar o mecanismo — exatamente
/// como a correção pediu. Prova as três cláusulas do Gherkin da correção: token válido autoriza;
/// token expirado (mais de 120s) nega com <c>AUTHORIZATION_REQUIRED</c>; token de ação diferente da
/// protegida nega com o mesmo código — o código antes "morto" em
/// <c>ResultExtensions.MapErrorCode</c> agora é realmente produzido.
/// </summary>
public sealed class AuthorizationTokenValidatorTests
{
    private const string TestSecret = "unit-test-jwt-secret-32-bytes-minimo!!";

    private readonly ITokenIssuer _tokenIssuer = new JwtTokenIssuer(Options.Create(new JwtOptions
    {
        Secret = TestSecret,
        Issuer = "nexora-test",
        Audience = "nexora-test-apps",
    }));

    private readonly IAuthorizationTokenValidator _validator;

    public AuthorizationTokenValidatorTests()
    {
        _validator = new AuthorizationTokenValidator(_tokenIssuer, NullLogger<AuthorizationTokenValidator>.Instance);
    }

    [Fact]
    public async Task Token_Ausente_Nega_Com_Authorization_Required()
    {
        var result = await _validator.ValidateAsync(null, "CANCEL_STARTED_ITEM");

        result.IsFailure.Should().BeTrue();
        result.Code.Should().Be(ApiErrorCodes.AuthorizationRequired);
    }

    [Fact]
    public async Task Token_Valido_Para_A_Acao_Certa_Autoriza_E_Devolve_Quem_Autorizou()
    {
        var authorizedBy = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var token = await IssueAsync("CANCEL_STARTED_ITEM", actorId, tenantId, storeId, deviceId, authorizedBy, ttlSeconds: 120);

        var result = await _validator.ValidateAsync(token, "CANCEL_STARTED_ITEM");

        result.IsSuccess.Should().BeTrue();
        result.Value!.AuthorizedBy.Should().Be(authorizedBy);
        result.Value.ActorId.Should().Be(actorId);
        result.Value.TenantId.Should().Be(tenantId);
        result.Value.StoreId.Should().Be(storeId);
        result.Value.DeviceId.Should().Be(deviceId);
        result.Value.Action.Should().Be("CANCEL_STARTED_ITEM");
    }

    [Fact]
    public async Task Token_Expirado_Ha_Mais_De_120_Segundos_Nega_Com_Authorization_Required()
    {
        // ttlSeconds negativo simula um token emitido há muito mais que os 120s de validade
        // (AuthTokenTtlSeconds.Authorization) sem precisar esperar de verdade num teste — o "exp"
        // já nasce no passado, bem fora da tolerância de ClockSkew (30s) de JwtTokenIssuer.
        var token = await IssueAsync(
            "CANCEL_STARTED_ITEM", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            ttlSeconds: -300);

        var result = await _validator.ValidateAsync(token, "CANCEL_STARTED_ITEM");

        result.IsFailure.Should().BeTrue();
        result.Code.Should().Be(ApiErrorCodes.AuthorizationRequired);
    }

    [Fact]
    public async Task Token_Emitido_Para_Outra_Acao_Nega_Com_Authorization_Required()
    {
        var token = await IssueAsync(
            "ADJUST_STOCK", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            ttlSeconds: 120);

        var result = await _validator.ValidateAsync(token, "CANCEL_STARTED_ITEM");

        result.IsFailure.Should().BeTrue();
        result.Code.Should().Be(ApiErrorCodes.AuthorizationRequired);
    }

    [Fact]
    public async Task Token_De_Acesso_Comum_Nao_Serve_Como_Token_De_Autorizacao()
    {
        // tokenUse="access" em vez de "authorization" — mesma defesa que ValidateRefreshTokenAsync
        // já tinha contra usar um token para um uso diferente do que foi emitido.
        var accessToken = await _tokenIssuer.IssueAccessTokenAsync(
            new AccessClaims(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Array.Empty<string>(), Array.Empty<string>()),
            ttlSeconds: 120);

        var result = await _validator.ValidateAsync(accessToken, "CANCEL_STARTED_ITEM");

        result.IsFailure.Should().BeTrue();
        result.Code.Should().Be(ApiErrorCodes.AuthorizationRequired);
    }

    private Task<string> IssueAsync(
        string action, Guid actorId, Guid tenantId, Guid storeId, Guid deviceId, Guid authorizedBy, int ttlSeconds)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = actorId.ToString(),
            ["tid"] = tenantId.ToString(),
            ["sid"] = storeId.ToString(),
            ["did"] = deviceId.ToString(),
            ["action"] = action,
            ["contextHash"] = "hash-de-teste",
            ["authorizedBy"] = authorizedBy.ToString(),
        };

        return _tokenIssuer.IssueAuthorizationTokenAsync(claims, ttlSeconds);
    }
}
