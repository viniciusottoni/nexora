using Nexora.Domain.Common;
using Nexora.Domain.Operation;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Operation;

/// <summary>Regras de negócio de <see cref="DiningTable"/> introduzidas/relevantes para a US-020.</summary>
public sealed class DiningTableTests
{
    private static DiningTable CreateTable() =>
        DiningTable.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "12", "token-inicial", seats: 4);

    [Fact]
    public void Create_Sem_Rotulo_Lanca_DomainException()
    {
        var act = () => DiningTable.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), " ", "token", 4);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Sem_Token_Lanca_DomainException()
    {
        var act = () => DiningTable.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "12", " ", 4);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Com_Zero_Assentos_Lanca_DomainException()
    {
        var act = () => DiningTable.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "12", "token", 0);

        act.Should().Throw<DomainException>();
    }

    /// <summary>Cenário Gherkin "Rotação de token": o token muda e o carimbo de atualização avança.</summary>
    [Fact]
    public void RotateQrToken_Substitui_O_Token_Anterior()
    {
        var table = CreateTable();
        var originalToken = table.QrToken;
        var originalUpdatedAt = table.UpdatedAt;

        table.RotateQrToken("token-novo-rotacionado");

        table.QrToken.Should().Be("token-novo-rotacionado");
        table.QrToken.Should().NotBe(originalToken);
        table.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public void RotateQrToken_Com_Token_Vazio_Lanca_DomainException()
    {
        var table = CreateTable();

        var act = () => table.RotateQrToken(" ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RotateQrToken_Com_Mesmo_Token_Lanca_DomainException()
    {
        var table = CreateTable();

        var act = () => table.RotateQrToken(table.QrToken);

        act.Should().Throw<DomainException>("o novo token precisa ser diferente do anterior — senão nada mudou de fato");
    }

    [Fact]
    public void Rename_Atualiza_Rotulo_Assentos_Area_E_Ordem()
    {
        var table = CreateTable();
        var newAreaId = Guid.NewGuid();

        table.Rename("V3", 6, newAreaId, 2);

        table.Label.Should().Be("V3");
        table.Seats.Should().Be(6);
        table.AreaId.Should().Be(newAreaId);
        table.SortOrder.Should().Be(2);
    }

    [Fact]
    public void Deactivate_Depois_Activate_Restaura_IsActive()
    {
        var table = CreateTable();

        table.Deactivate();
        table.IsActive.Should().BeFalse();

        table.Activate();
        table.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Cenário Gherkin "Abertura pelo garçom" (US-022 §4): "a mesa deve aparecer como ocupada no
    /// mapa" — transição de estado FREE -&gt; OCCUPIED que acontece junto da abertura de sessão.
    /// </summary>
    [Fact]
    public void Occupy_Muda_O_Estado_Para_Occupied()
    {
        var table = CreateTable();
        table.Status.Should().Be(TableStatus.Free);

        table.Occupy();

        table.Status.Should().Be(TableStatus.Occupied);
    }

    [Fact]
    public void Occupy_Mesa_Bloqueada_Lanca_DomainException()
    {
        var table = CreateTable();
        table.Block();

        var act = table.Occupy;

        act.Should().Throw<DomainException>();
    }

    /// <summary>Liberação da mesa (fechamento de comanda, E-05) devolve o estado a FREE, mesmo vindo de OCCUPIED.</summary>
    [Fact]
    public void Release_Devolve_A_Mesa_Para_Free()
    {
        var table = CreateTable();
        table.Occupy();

        table.Release();

        table.Status.Should().Be(TableStatus.Free);
    }
}
