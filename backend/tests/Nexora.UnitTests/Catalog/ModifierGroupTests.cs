using Nexora.Domain.Catalog;
using Nexora.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Catalog;

/// <summary>
/// US-012 §12 ("Unitário: validação de mínimo, máximo, seleção única e obrigatoriedade") — regra
/// de <see cref="ModifierGroup"/> isolada de banco/HTTP. Cenário Gherkin "Modificador obrigatório"
/// e "Limite máximo de seleção" nascem destas invariantes: um grupo malformado (max &lt; min, ou
/// negativo) nunca deveria existir para o cliente esbarrar depois no carrinho.
/// </summary>
public sealed class ModifierGroupTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_Com_Nome_Vazio_Lanca_DomainException()
    {
        var act = () => ModifierGroup.Create(TenantId, string.Empty);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Com_MinSelect_Negativo_Lanca_DomainException()
    {
        var act = () => ModifierGroup.Create(TenantId, "Tamanho", minSelect: -1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Com_MaxSelect_Menor_Que_MinSelect_Lanca_DomainException()
    {
        var act = () => ModifierGroup.Create(TenantId, "Adicionais", minSelect: 3, maxSelect: 1);

        act.Should().Throw<DomainException>();
    }

    /// <summary>Cenário Gherkin "Modificador obrigatório": grupo "Tamanho" obrigatório, seleção única (min=max=1).</summary>
    [Fact]
    public void Create_Grupo_Obrigatorio_De_Selecao_Unica_Mantem_Min_E_Max_Iguais_A_Um()
    {
        var group = ModifierGroup.Create(TenantId, "Tamanho", minSelect: 1, maxSelect: 1, isRequired: true);

        group.IsRequired.Should().BeTrue();
        group.MinSelect.Should().Be(1);
        group.MaxSelect.Should().Be(1);
    }

    [Fact]
    public void Create_Grupo_Obrigatorio_Com_Minimo_Zero_Lanca_DomainException()
    {
        var act = () => ModifierGroup.Create(TenantId, "Tamanho", minSelect: 0, maxSelect: 1, isRequired: true);

        act.Should().Throw<DomainException>()
            .WithMessage("*grupo obrigatório*");
    }

    /// <summary>Cenário Gherkin "Limite máximo de seleção": grupo "Adicionais" com máximo de 3 opções.</summary>
    [Fact]
    public void Create_Grupo_Opcional_Com_Limite_Maximo_De_Tres_Opcoes()
    {
        var group = ModifierGroup.Create(TenantId, "Adicionais", minSelect: 0, maxSelect: 3, isRequired: false);

        group.IsRequired.Should().BeFalse();
        group.MinSelect.Should().Be(0);
        group.MaxSelect.Should().Be(3);
    }

    [Fact]
    public void Create_Usa_Valores_Padrao_Quando_Nao_Informados()
    {
        var group = ModifierGroup.Create(TenantId, "Ponto da massa");

        group.MinSelect.Should().Be(0);
        group.MaxSelect.Should().Be(1);
        group.IsRequired.Should().BeFalse();
        group.SortOrder.Should().Be(0);
        group.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateSelectionRange_Com_Valores_Validos_Atualiza_Min_E_Max()
    {
        var group = ModifierGroup.Create(TenantId, "Adicionais", minSelect: 0, maxSelect: 1);
        var updatedAtBefore = group.UpdatedAt;

        group.UpdateSelectionRange(1, 5);

        group.MinSelect.Should().Be(1);
        group.MaxSelect.Should().Be(5);
        group.UpdatedAt.Should().BeOnOrAfter(updatedAtBefore);
    }

    [Fact]
    public void UpdateSelectionRange_Com_MaxSelect_Menor_Que_MinSelect_Lanca_DomainException()
    {
        var group = ModifierGroup.Create(TenantId, "Adicionais");

        var act = () => group.UpdateSelectionRange(5, 1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateSelectionRange_Grupo_Obrigatorio_Com_Minimo_Zero_Lanca_DomainException()
    {
        var group = ModifierGroup.Create(TenantId, "Tamanho", minSelect: 1, maxSelect: 1, isRequired: true);

        var act = () => group.UpdateSelectionRange(0, 1);

        act.Should().Throw<DomainException>()
            .WithMessage("*grupo obrigatório*");
    }

    /// <summary>
    /// RN "grupo reusado em N produtos": <see cref="ModifierGroup.UpdateSelectionRange"/> muda um
    /// único agregado, sem cópia por produto — a Application (não Domain) é quem propaga o efeito
    /// para os produtos vinculados via <c>ProductModifierGroup</c> (ver
    /// UpdateModifierGroupCommandHandler, que emite <c>product.updated</c> para cada produto).
    /// </summary>
    [Fact]
    public void SoftDelete_Marca_DeletedAt_Sem_Remover_Fisicamente()
    {
        var group = ModifierGroup.Create(TenantId, "Adicionais");

        group.SoftDelete();

        group.DeletedAt.Should().NotBeNull();
    }
}
