using Nexora.Domain.Common;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-145 "Acesso de suporte auditado" — regra de negócio pura de <see cref="SupportAccess"/>
/// (sem banco): concessão exige motivo/duração/token, expiração é derivada da duração concedida,
/// revogação é idempotente e <see cref="SupportAccess.IsActive"/> combina os dois (cenário Gherkin
/// "Nenhum acesso sem registro" depende exatamente desta combinação).
/// </summary>
public sealed class SupportAccessTests
{
    private static SupportAccess CreateGrant(DateTimeOffset grantedAt, int durationMinutes = 60) =>
        SupportAccess.Grant(Guid.NewGuid(), Guid.NewGuid(), "Investigação de chamado #482", durationMinutes, "hash-1", grantedAt);

    [Fact]
    public void Grant_Calcula_ExpiresAt_A_Partir_Da_Duracao()
    {
        var grantedAt = DateTimeOffset.UtcNow;

        var access = CreateGrant(grantedAt, durationMinutes: 90);

        access.ExpiresAt.Should().Be(grantedAt.AddMinutes(90));
        access.GrantedAt.Should().Be(grantedAt);
        access.TokenHash.Should().Be("hash-1");
        access.IsRevoked.Should().BeFalse();
        access.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Grant_Sem_Motivo_Lanca_DomainException()
    {
        var act = () => SupportAccess.Grant(Guid.NewGuid(), null, "  ", 60, "hash-1", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Grant_Com_Duracao_Zero_Ou_Negativa_Lanca_DomainException()
    {
        var act = () => SupportAccess.Grant(Guid.NewGuid(), null, "Motivo", 0, "hash-1", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Grant_Sem_TokenHash_Lanca_DomainException()
    {
        var act = () => SupportAccess.Grant(Guid.NewGuid(), null, "Motivo", 60, "  ", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IsExpired_Antes_Do_Prazo_E_Falso()
    {
        var grantedAt = DateTimeOffset.UtcNow;
        var access = CreateGrant(grantedAt, durationMinutes: 30);

        access.IsExpired(grantedAt.AddMinutes(29)).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_No_Instante_Exato_Do_Prazo_E_Verdadeiro()
    {
        var grantedAt = DateTimeOffset.UtcNow;
        var access = CreateGrant(grantedAt, durationMinutes: 30);

        access.IsExpired(grantedAt.AddMinutes(30)).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_Apos_O_Prazo_E_Verdadeiro()
    {
        var grantedAt = DateTimeOffset.UtcNow;
        var access = CreateGrant(grantedAt, durationMinutes: 30);

        access.IsExpired(grantedAt.AddMinutes(31)).Should().BeTrue();
    }

    [Fact]
    public void Revoke_Marca_RevokedAt_E_RevokedBy()
    {
        var access = CreateGrant(DateTimeOffset.UtcNow);
        var revokedBy = Guid.NewGuid();
        var revokedAt = DateTimeOffset.UtcNow.AddMinutes(5);

        access.Revoke(revokedBy, revokedAt);

        access.IsRevoked.Should().BeTrue();
        access.RevokedAt.Should().Be(revokedAt);
        access.RevokedBy.Should().Be(revokedBy);
    }

    [Fact]
    public void Revoke_Chamado_Duas_Vezes_E_Idempotente_Mantem_O_Primeiro_Registro()
    {
        var access = CreateGrant(DateTimeOffset.UtcNow);
        var firstRevokedBy = Guid.NewGuid();
        var firstRevokedAt = DateTimeOffset.UtcNow.AddMinutes(5);

        access.Revoke(firstRevokedBy, firstRevokedAt);
        access.Revoke(Guid.NewGuid(), firstRevokedAt.AddMinutes(10));

        access.RevokedAt.Should().Be(firstRevokedAt);
        access.RevokedBy.Should().Be(firstRevokedBy);
    }

    [Fact]
    public void IsActive_Concessao_Recente_Nao_Expirada_Nem_Revogada_E_Verdadeiro()
    {
        var grantedAt = DateTimeOffset.UtcNow;
        var access = CreateGrant(grantedAt, durationMinutes: 60);

        access.IsActive(grantedAt.AddMinutes(10)).Should().BeTrue();
    }

    [Fact]
    public void IsActive_Apos_Expiracao_E_Falso()
    {
        var grantedAt = DateTimeOffset.UtcNow;
        var access = CreateGrant(grantedAt, durationMinutes: 60);

        access.IsActive(grantedAt.AddMinutes(61)).Should().BeFalse();
    }

    [Fact]
    public void IsActive_Apos_Revogacao_Mesmo_Antes_Do_Prazo_E_Falso()
    {
        var grantedAt = DateTimeOffset.UtcNow;
        var access = CreateGrant(grantedAt, durationMinutes: 60);
        access.Revoke(Guid.NewGuid(), grantedAt.AddMinutes(5));

        access.IsActive(grantedAt.AddMinutes(6)).Should().BeFalse();
    }

    [Fact]
    public void RecordUsage_Atualiza_LastUsedAt()
    {
        var access = CreateGrant(DateTimeOffset.UtcNow);
        var usedAt = DateTimeOffset.UtcNow.AddMinutes(2);

        access.RecordUsage(usedAt);

        access.LastUsedAt.Should().Be(usedAt);
    }
}
