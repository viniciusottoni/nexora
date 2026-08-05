using Nexora.Domain.Common;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-155 "Proprietários, usuários iniciais e convites" — <see cref="OwnershipTransfer"/> é um
/// registro de fato imutável (mesma natureza de <see cref="AuditLog"/>/<see cref="DomainEvent"/>):
/// as únicas regras são as invariantes mínimas de criação.
/// </summary>
public sealed class OwnershipTransferTests
{
    [Fact]
    public void Create_Preenche_Todos_Os_Campos()
    {
        var tenantId = Guid.NewGuid();
        var previousOwnerId = Guid.NewGuid();
        var newOwnerId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var transferredAt = DateTimeOffset.UtcNow;

        var transfer = OwnershipTransfer.Create(
            tenantId, previousOwnerId, newOwnerId, "Alteração societária", previousKeptAsAdmin: true, actorId, transferredAt);

        transfer.Id.Should().NotBe(Guid.Empty);
        transfer.TenantId.Should().Be(tenantId);
        transfer.PreviousOwnerUserId.Should().Be(previousOwnerId);
        transfer.NewOwnerUserId.Should().Be(newOwnerId);
        transfer.Reason.Should().Be("Alteração societária");
        transfer.PreviousKeptAsAdmin.Should().BeTrue();
        transfer.ActorId.Should().Be(actorId);
        transfer.TransferredAt.Should().Be(transferredAt);
    }

    [Fact]
    public void Create_Sem_Motivo_Lanca_DomainException()
    {
        var act = () => OwnershipTransfer.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "   ", false, null, DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    /// <summary>Invariante mínima: transferir "para si mesmo" não é uma transferência.</summary>
    [Fact]
    public void Create_Com_Mesmo_Proprietario_Anterior_E_Novo_Lanca_DomainException()
    {
        var sameUserId = Guid.NewGuid();

        var act = () => OwnershipTransfer.Create(
            Guid.NewGuid(), sameUserId, sameUserId, "Motivo qualquer", false, null, DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }
}
