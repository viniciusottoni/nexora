using Nexora.Application.Platform.SupportAccessTokens;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-145, cenário Gherkin "Nenhum acesso sem registro" — a decisão pura de
/// <see cref="SupportAccessTokenPolicy"/> (sem banco, ver docstring da classe) para os três motivos
/// de recusa que <see cref="Application.Platform.SupportAccessTokens.ISupportAccessTokenValidator"/>
/// precisa distinguir: token desconhecido, expirado e revogado — cada um com código de erro
/// próprio (Shared/Errors/ApiErrorCodes.SupportAccess.cs).
/// </summary>
public sealed class SupportAccessTokenPolicyTests
{
    private static SupportAccess Grant(DateTimeOffset grantedAt, int durationMinutes = 60) =>
        SupportAccess.Grant(Guid.NewGuid(), Guid.NewGuid(), "Motivo", durationMinutes, "hash-1", grantedAt);

    [Fact]
    public void Evaluate_Token_Desconhecido_Retorna_NotFound()
    {
        var status = SupportAccessTokenPolicy.Evaluate(null, DateTimeOffset.UtcNow);

        status.Should().Be(SupportAccessTokenStatus.NotFound);
    }

    [Fact]
    public void Evaluate_Token_Revogado_Retorna_Revoked_Mesmo_Antes_Do_Prazo()
    {
        var grantedAt = DateTimeOffset.UtcNow;
        var access = Grant(grantedAt, durationMinutes: 60);
        access.Revoke(Guid.NewGuid(), grantedAt.AddMinutes(5));

        var status = SupportAccessTokenPolicy.Evaluate(access, grantedAt.AddMinutes(6));

        status.Should().Be(SupportAccessTokenStatus.Revoked);
    }

    [Fact]
    public void Evaluate_Token_Apos_O_Prazo_Retorna_Expired()
    {
        var grantedAt = DateTimeOffset.UtcNow;
        var access = Grant(grantedAt, durationMinutes: 30);

        var status = SupportAccessTokenPolicy.Evaluate(access, grantedAt.AddMinutes(31));

        status.Should().Be(SupportAccessTokenStatus.Expired);
    }

    [Fact]
    public void Evaluate_Token_Ativo_Retorna_Valid()
    {
        var grantedAt = DateTimeOffset.UtcNow;
        var access = Grant(grantedAt, durationMinutes: 30);

        var status = SupportAccessTokenPolicy.Evaluate(access, grantedAt.AddMinutes(10));

        status.Should().Be(SupportAccessTokenStatus.Valid);
    }

    [Theory]
    [InlineData(SupportAccessTokenStatus.NotFound, ApiErrorCodes.SupportAccessTokenNotFound)]
    [InlineData(SupportAccessTokenStatus.Revoked, ApiErrorCodes.SupportAccessTokenRevoked)]
    [InlineData(SupportAccessTokenStatus.Expired, ApiErrorCodes.SupportAccessTokenExpired)]
    public void FailureFor_Cada_Status_De_Falha_Tem_Codigo_Proprio(SupportAccessTokenStatus status, string expectedCode)
    {
        var (_, code) = SupportAccessTokenPolicy.FailureFor(status);

        code.Should().Be(expectedCode);
    }

    [Fact]
    public void FailureFor_Valid_Lanca_InvalidOperationException()
    {
        var act = () => SupportAccessTokenPolicy.FailureFor(SupportAccessTokenStatus.Valid);

        act.Should().Throw<InvalidOperationException>();
    }
}
