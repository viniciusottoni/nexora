using Nexora.Domain.Catalog;
using Nexora.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Catalog;

/// <summary>
/// US-012 §12 ("Unitário: cálculo de preço do item com múltiplos adicionais e deltas negativos") —
/// regra de <see cref="Modifier"/> isolada de banco/HTTP. Cenários Gherkin "Preço do adicional
/// somado" e "Remoção sem custo" nascem do próprio valor de <c>PriceDelta</c>; a soma de vários
/// modificadores num item de pedido é responsabilidade do módulo de pedidos (fora desta US), mas o
/// dado que ele soma — o delta em si — é validado aqui.
/// </summary>
public sealed class ModifierTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();

    [Fact]
    public void Create_Com_Nome_Vazio_Lanca_DomainException()
    {
        var act = () => Modifier.Create(TenantId, GroupId, string.Empty);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Sem_PriceDelta_Usa_Zero_E_Fica_Disponivel_Por_Padrao()
    {
        var modifier = Modifier.Create(TenantId, GroupId, "Sem cebola");

        modifier.PriceDelta.Should().Be(0m);
        modifier.IsAvailable.Should().BeTrue();
        modifier.DeletedAt.Should().BeNull();
    }

    /// <summary>Cenário Gherkin "Preço do adicional somado": adicional "Borda Catupiry" de R$ 8,00.</summary>
    [Fact]
    public void Create_Com_PriceDelta_Positivo_Representa_Um_Adicional()
    {
        var modifier = Modifier.Create(TenantId, GroupId, "Borda Catupiry", priceDelta: 8.00m);

        modifier.PriceDelta.Should().Be(8.00m);
    }

    /// <summary>Cenário Gherkin "Remoção sem custo": "sem cebola" com price_delta zero — não muda o preço do item.</summary>
    [Fact]
    public void Create_Com_PriceDelta_Zero_Representa_Uma_Remocao_Sem_Custo()
    {
        var modifier = Modifier.Create(TenantId, GroupId, "Sem cebola", priceDelta: 0m);

        modifier.PriceDelta.Should().Be(0m);
    }

    /// <summary>Doc §3.1 "price_delta (positivo, zero ou negativo)" — desconto/ajuste para baixo também é um price_delta válido.</summary>
    [Fact]
    public void Create_Com_PriceDelta_Negativo_E_Permitido()
    {
        var modifier = Modifier.Create(TenantId, GroupId, "Desconto meia porção", priceDelta: -3.50m);

        modifier.PriceDelta.Should().Be(-3.50m);
    }

    [Fact]
    public void Create_Com_Insumo_Guarda_IngredientId_E_Quantity()
    {
        var ingredientId = Guid.NewGuid();

        var modifier = Modifier.Create(TenantId, GroupId, "Bacon extra", priceDelta: 5m, ingredientId: ingredientId, quantity: 0.05m);

        modifier.IngredientId.Should().Be(ingredientId);
        modifier.Quantity.Should().Be(0.05m);
    }

    [Fact]
    public void MarkUnavailable_Depois_MarkAvailable_Restaura_Disponibilidade()
    {
        var modifier = Modifier.Create(TenantId, GroupId, "Borda Catupiry", priceDelta: 8m);

        modifier.MarkUnavailable();
        modifier.IsAvailable.Should().BeFalse();

        modifier.MarkAvailable();
        modifier.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void UpdatePrice_Altera_PriceDelta()
    {
        var modifier = Modifier.Create(TenantId, GroupId, "Borda Catupiry", priceDelta: 8m);

        modifier.UpdatePrice(9.50m);

        modifier.PriceDelta.Should().Be(9.50m);
    }

    [Fact]
    public void SoftDelete_Marca_DeletedAt_Sem_Remover_Fisicamente()
    {
        var modifier = Modifier.Create(TenantId, GroupId, "Borda Catupiry", priceDelta: 8m);

        modifier.SoftDelete();

        modifier.DeletedAt.Should().NotBeNull();
    }
}
