using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-002, gap "AppUser.Invite usa UserStatus.Inactive, mas a spec exige status=INVITED"
/// (Docs/Domain/12-Seeds-e-Dados-Iniciais.md §8). Cobre a criação do convite e a transição para
/// <see cref="UserStatus.Active"/> no aceite (<see cref="AppUser.SetPassword"/>), que é o mesmo
/// caminho usado por <c>AcceptOwnerInvitationCommandHandler</c>.
/// </summary>
public sealed class AppUserInviteTests
{
    [Fact]
    public void Invite_Cria_Usuario_Com_Status_Invited_E_Sem_Credencial()
    {
        var tenantId = Guid.NewGuid();

        var user = AppUser.Invite(tenantId, "Dona Betinha", "betinha@example.com");

        user.TenantId.Should().Be(tenantId);
        user.Status.Should().Be(UserStatus.Invited);
        user.PasswordHash.Should().BeNull();
        user.PinHash.Should().BeNull();
    }

    [Fact]
    public void SetPassword_Transiciona_De_Invited_Para_Active()
    {
        var user = AppUser.Invite(Guid.NewGuid(), "Dona Betinha", "betinha@example.com");
        user.Status.Should().Be(UserStatus.Invited);

        user.SetPassword("hash-da-senha");

        user.Status.Should().Be(UserStatus.Active);
        user.PasswordHash.Should().Be("hash-da-senha");
    }

    [Fact]
    public void Invite_Sem_Email_Lanca_DomainException()
    {
        var act = () => AppUser.Invite(Guid.NewGuid(), "Dona Betinha", string.Empty);

        act.Should().Throw<Nexora.Domain.Common.DomainException>();
    }
}
