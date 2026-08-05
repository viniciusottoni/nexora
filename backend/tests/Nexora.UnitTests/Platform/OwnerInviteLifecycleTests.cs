using Nexora.Domain.Common;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-155 "Proprietários, usuários iniciais e convites" — regra de negócio pura de
/// <see cref="OwnerInvite"/> (sem banco): expiração, revogação e a máquina de estados
/// PENDING/ACCEPTED/EXPIRED/REVOKED (<see cref="OwnerInvite.ResolveStatus"/>) que sustenta os
/// cenários Gherkin "Convite expirado" e "Segredo não recuperável".
/// </summary>
public sealed class OwnerInviteLifecycleTests
{
    private static OwnerInvite CreateInvite(DateTimeOffset expiresAt) =>
        OwnerInvite.Create(Guid.NewGuid(), Guid.NewGuid(), "owner@example.com", "hash-1", expiresAt);

    [Fact]
    public void Create_Sem_Motivo_Fica_Com_Reason_Nulo()
    {
        var invite = CreateInvite(DateTimeOffset.UtcNow.AddHours(72));

        invite.Reason.Should().BeNull();
        invite.EmailOutboxId.Should().BeNull();
        invite.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void Create_Com_Motivo_E_OutboxId_Preenche_Os_Dois()
    {
        var outboxId = Guid.NewGuid();
        var invite = OwnerInvite.Create(
            Guid.NewGuid(), Guid.NewGuid(), "owner@example.com", "hash-1", DateTimeOffset.UtcNow.AddHours(72),
            reason: "Correção solicitada no chamado #91", emailOutboxId: outboxId);

        invite.Reason.Should().Be("Correção solicitada no chamado #91");
        invite.EmailOutboxId.Should().Be(outboxId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_Sem_Email_Lanca_DomainException(string? email)
    {
        var act = () => OwnerInvite.Create(Guid.NewGuid(), Guid.NewGuid(), email!, "hash-1", DateTimeOffset.UtcNow.AddHours(1));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ResolveStatus_Pendente_Antes_Da_Expiracao()
    {
        var now = DateTimeOffset.UtcNow;
        var invite = CreateInvite(now.AddHours(72));

        invite.ResolveStatus(now).Should().Be("PENDING");
    }

    /// <summary>Cenário Gherkin "Convite expirado".</summary>
    [Fact]
    public void ResolveStatus_Expirado_Depois_Do_Prazo()
    {
        var now = DateTimeOffset.UtcNow;
        var invite = CreateInvite(now.AddHours(-1));

        invite.IsExpired(now).Should().BeTrue();
        invite.ResolveStatus(now).Should().Be("EXPIRED");
    }

    [Fact]
    public void Consume_Marca_Como_Aceito_E_Nao_Pode_Ser_Consumido_De_Novo()
    {
        var now = DateTimeOffset.UtcNow;
        var invite = CreateInvite(now.AddHours(72));

        invite.Consume();

        invite.IsConsumed.Should().BeTrue();
        invite.ResolveStatus(now).Should().Be("ACCEPTED");

        var act = () => invite.Consume();
        act.Should().Throw<DomainException>();
    }

    /// <summary>Reenvio: "qualquer token anterior deve ser invalidado" — mecanismo é <see cref="OwnerInvite.Revoke"/>.</summary>
    [Fact]
    public void Revoke_Marca_Como_Revogado_Com_Motivo()
    {
        var now = DateTimeOffset.UtcNow;
        var invite = CreateInvite(now.AddHours(72));

        invite.Revoke("Reenvio solicitado — endereço incorreto");

        invite.IsRevoked.Should().BeTrue();
        invite.RevokedReason.Should().Be("Reenvio solicitado — endereço incorreto");
        invite.RevokedAt.Should().NotBeNull();
        invite.ResolveStatus(now).Should().Be("REVOKED");
    }

    [Fact]
    public void Revoke_Sem_Motivo_Lanca_DomainException()
    {
        var invite = CreateInvite(DateTimeOffset.UtcNow.AddHours(72));

        var act = () => invite.Revoke("   ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Revoke_Convite_Ja_Consumido_Lanca_DomainException()
    {
        var invite = CreateInvite(DateTimeOffset.UtcNow.AddHours(72));
        invite.Consume();

        var act = () => invite.Revoke("Motivo qualquer");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Revoke_Convite_Ja_Revogado_Lanca_DomainException()
    {
        var invite = CreateInvite(DateTimeOffset.UtcNow.AddHours(72));
        invite.Revoke("Primeira revogação");

        var act = () => invite.Revoke("Segunda revogação");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Revoke_Convite_Expirado_Ainda_E_Permitido_E_Prioriza_Revoked_No_Status()
    {
        var now = DateTimeOffset.UtcNow;
        var invite = CreateInvite(now.AddHours(-1));

        invite.Revoke("Superado por reenvio");

        invite.ResolveStatus(now).Should().Be("REVOKED");
    }
}
