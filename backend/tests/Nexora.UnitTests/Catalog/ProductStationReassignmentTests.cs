using Nexora.Domain.Catalog;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Catalog;

/// <summary>
/// US-016 — <see cref="Product.ReassignStation"/> (método novo desta tarefa: antes só era
/// possível definir <c>StationId</c> na criação, via <see cref="Product.Create"/>). Cobre
/// atribuição, remoção e reatribuição, isolado de banco/HTTP.
/// </summary>
public sealed class ProductStationReassignmentTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Fact]
    public void ReassignStation_De_Produto_Sem_Praca_Atribui_A_Praca_Informada()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Mussarela");
        var stationId = Guid.NewGuid();

        product.ReassignStation(stationId);

        product.StationId.Should().Be(stationId);
    }

    /// <summary>Cenário Gherkin "Roteamento pela praça": trocar de praça (ex.: Forno -> Chapa) substitui, não acumula.</summary>
    [Fact]
    public void ReassignStation_Substitui_A_Praca_Anterior()
    {
        var forno = Guid.NewGuid();
        var chapa = Guid.NewGuid();
        var product = Product.Create(TenantId, CategoryId, "Pizza Mussarela", stationId: forno);

        product.ReassignStation(chapa);

        product.StationId.Should().Be(chapa);
    }

    [Fact]
    public void ReassignStation_Com_Null_Remove_O_Vinculo_Com_A_Praca()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Mussarela", stationId: Guid.NewGuid());

        product.ReassignStation(null);

        product.StationId.Should().BeNull();
    }

    [Fact]
    public void ReassignStation_Atualiza_UpdatedAt()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Mussarela");
        var before = product.UpdatedAt;

        product.ReassignStation(Guid.NewGuid());

        product.UpdatedAt.Should().BeOnOrAfter(before);
    }
}
