using Nexora.Application.Abstractions.Security;
using Nexora.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Xunit;

namespace Nexora.UnitTests.Auth;

/// <summary>
/// US-021 §3.1/§7: "Emissão de token anônimo de sessão, com escopo mínimo e expiração junto da
/// sessão da mesa". Cobre <see cref="JwtTokenIssuer.IssueTableSessionTokenAsync"/>/
/// <see cref="JwtTokenIssuer.ValidateTableSessionTokenAsync"/> contra o emissor REAL (mesmo usado
/// em produção) — inclusive o requisito de segurança da US-021 §12 ("sessionToken não permite
/// acesso a outra mesa nem a rota administrativa"): aqui verificado no nível do token em si (não
/// carrega roles/perms de staff e não valida como nenhum outro tipo de token desta solution).
/// </summary>
public sealed class TableSessionTokenIssuerTests
{
    private const string TestSecret = "unit-test-jwt-secret-32-bytes-minimo!!";

    private readonly ITokenIssuer _tokenIssuer = new JwtTokenIssuer(Options.Create(new JwtOptions
    {
        Secret = TestSecret,
        Issuer = "nexora-test",
        Audience = "nexora-test-apps",
    }));

    [Fact]
    public async Task Emite_E_Valida_Token_Com_As_Claims_Minimas_Da_Sessao()
    {
        var sessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var tableId = Guid.NewGuid();

        var token = await _tokenIssuer.IssueTableSessionTokenAsync(sessionId, tenantId, tableId, ttlSeconds: 3600);
        var claims = await _tokenIssuer.ValidateTableSessionTokenAsync(token);

        claims.SessionId.Should().Be(sessionId);
        claims.TenantId.Should().Be(tenantId);
        claims.TableId.Should().Be(tableId);
    }

    [Fact]
    public async Task Token_Expirado_Falha_Na_Validacao()
    {
        var token = await _tokenIssuer.IssueTableSessionTokenAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ttlSeconds: -300);

        var act = () => _tokenIssuer.ValidateTableSessionTokenAsync(token);

        await act.Should().ThrowAsync<SecurityTokenException>();
    }

    /// <summary>
    /// RN-015/US-021 §12: um token comum de staff (emitido para login por PIN/senha, com
    /// roles/perms) nunca é aceito como token de sessão de mesa — mesmo assinado pela mesma
    /// chave, o <c>tokenUse</c> diferente ("access" em vez de "table_session") é rejeitado.
    /// </summary>
    [Fact]
    public async Task Token_De_Acesso_De_Staff_Nao_Serve_Como_Token_De_Sessao_De_Mesa()
    {
        var accessToken = await _tokenIssuer.IssueAccessTokenAsync(
            new AccessClaims(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new[] { "GARCOM" }, new[] { "table:open" }),
            ttlSeconds: 3600);

        var act = () => _tokenIssuer.ValidateTableSessionTokenAsync(accessToken);

        await act.Should().ThrowAsync<SecurityTokenException>();
    }

    /// <summary>
    /// Simetricamente: um token de sessão de mesa não serve como refresh token nem como token de
    /// autorização pontual — "escopo mínimo" significa que ele só é aceito por
    /// <see cref="ITokenIssuer.ValidateTableSessionTokenAsync"/>, nunca pelos demais validadores.
    /// </summary>
    [Fact]
    public async Task Token_De_Sessao_De_Mesa_Nao_Serve_Como_Refresh_Nem_Como_Autorizacao()
    {
        var token = await _tokenIssuer.IssueTableSessionTokenAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ttlSeconds: 3600);

        var actRefresh = () => _tokenIssuer.ValidateRefreshTokenAsync(token);
        var actAuthorization = () => _tokenIssuer.ValidateAuthorizationTokenAsync(token);

        await actRefresh.Should().ThrowAsync<SecurityTokenException>();
        await actAuthorization.Should().ThrowAsync<SecurityTokenException>();
    }

    /// <summary>
    /// Duas mesas diferentes recebem claims diferentes — a base de qualquer checagem futura de
    /// "este token só serve para ESTA mesa/sessão" (RN-015, "nunca leitura de outra mesa").
    /// </summary>
    [Fact]
    public async Task Tokens_De_Sessoes_Diferentes_Carregam_Claims_Diferentes()
    {
        var tenantId = Guid.NewGuid();
        var (sessionA, tableA) = (Guid.NewGuid(), Guid.NewGuid());
        var (sessionB, tableB) = (Guid.NewGuid(), Guid.NewGuid());

        var tokenA = await _tokenIssuer.IssueTableSessionTokenAsync(sessionA, tenantId, tableA, ttlSeconds: 3600);
        var tokenB = await _tokenIssuer.IssueTableSessionTokenAsync(sessionB, tenantId, tableB, ttlSeconds: 3600);

        var claimsFromTokenA = await _tokenIssuer.ValidateTableSessionTokenAsync(tokenA);

        claimsFromTokenA.SessionId.Should().NotBe(sessionB);
        claimsFromTokenA.TableId.Should().NotBe(tableB);
        tokenA.Should().NotBe(tokenB);
    }
}
