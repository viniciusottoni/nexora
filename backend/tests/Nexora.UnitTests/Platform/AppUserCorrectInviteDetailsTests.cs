using Nexora.Domain.Common;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-155 "Proprietários, usuários iniciais e convites" — <see cref="AppUser.CorrectInviteDetails"/>
/// só pode corrigir nome/e-mail ANTES da aceitação (Gherkin "E-mail corrigido antes da aceitação");
/// depois que <see cref="AppUser.SetPassword"/> roda (aceite do convite), a correção deixa de fazer
/// sentido — o e-mail vira credencial de login de um usuário já ativo.
/// </summary>
public sealed class AppUserCorrectInviteDetailsTests
{
    [Fact]
    public void CorrectInviteDetails_Enquanto_Invited_Atualiza_Nome_E_Email()
    {
        var user = AppUser.Invite(Guid.NewGuid(), "Nome Errado", "errado@example.com");

        user.CorrectInviteDetails("Nome Certo", "CERTO@Example.com");

        user.Name.Should().Be("Nome Certo");
        user.Email.Should().Be("certo@example.com");
        user.Status.Should().Be(UserStatus.Invited);
    }

    [Fact]
    public void CorrectInviteDetails_Depois_De_Aceito_Lanca_DomainException()
    {
        var user = AppUser.Invite(Guid.NewGuid(), "Dona Betinha", "betinha@example.com");
        user.SetPassword("hash-da-senha");

        var act = () => user.CorrectInviteDetails("Outro Nome", "outro@example.com");

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("", "email@example.com")]
    [InlineData("Nome", "")]
    public void CorrectInviteDetails_Com_Campo_Vazio_Lanca_DomainException(string name, string email)
    {
        var user = AppUser.Invite(Guid.NewGuid(), "Dona Betinha", "betinha@example.com");

        var act = () => user.CorrectInviteDetails(name, email);

        act.Should().Throw<DomainException>();
    }
}
