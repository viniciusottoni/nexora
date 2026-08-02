using Nexora.Domain.Common;
using Nexora.Domain.Operation;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Operation;

public sealed class AreaTests
{
    [Fact]
    public void Create_Sem_Nome_Lanca_DomainException()
    {
        var act = () => Area.Create(Guid.NewGuid(), Guid.NewGuid(), " ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rename_Atualiza_Nome_E_Ordem()
    {
        var area = Area.Create(Guid.NewGuid(), Guid.NewGuid(), "Salão");

        area.Rename("Varanda", 3);

        area.Name.Should().Be("Varanda");
        area.SortOrder.Should().Be(3);
    }

    [Fact]
    public void Rename_Sem_Nome_Lanca_DomainException()
    {
        var area = Area.Create(Guid.NewGuid(), Guid.NewGuid(), "Salão");

        var act = () => area.Rename(" ", 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Deactivate_Depois_Activate_Restaura_IsActive()
    {
        var area = Area.Create(Guid.NewGuid(), Guid.NewGuid(), "Salão");

        area.Deactivate();
        area.IsActive.Should().BeFalse();

        area.Activate();
        area.IsActive.Should().BeTrue();
    }
}
