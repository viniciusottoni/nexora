using Nexora.Domain.Catalog;
using Nexora.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Catalog;

/// <summary>
/// US-010 (Cadastrar categorias e produtos) §12 ("Unitário: validação de campos obrigatórios e de
/// ordenação") — cobre as invariantes de <see cref="Product"/> isoladas de banco/HTTP.
/// <see cref="Product.MarkAvailable"/>/<see cref="Product.MarkUnavailable"/> são escopo da US-015
/// (indisponibilidade operacional) e não são cobertos aqui.
/// </summary>
public sealed class ProductTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Fact]
    public void Create_Com_Nome_Vazio_Lanca_DomainException()
    {
        var act = () => Product.Create(TenantId, CategoryId, name: "   ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Com_MaxFractions_Menor_Que_Um_Lanca_DomainException()
    {
        var act = () => Product.Create(TenantId, CategoryId, "Pizza Mussarela", allowsFractions: true, maxFractions: 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Preenche_Campos_Padrao_E_Fica_Ativo_E_Disponivel_Por_Padrao()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Mussarela", description: "Molho, mussarela, orégano");

        product.Name.Should().Be("Pizza Mussarela");
        product.Description.Should().Be("Molho, mussarela, orégano");
        product.IsActive.Should().BeTrue();
        product.IsAvailable.Should().BeTrue();
        product.StationId.Should().BeNull();
        product.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Create_Aceita_Praca_E_Alergenos_Opcionais()
    {
        var stationId = Guid.NewGuid();

        var product = Product.Create(
            TenantId, CategoryId, "Pizza Mussarela",
            stationId: stationId,
            allergens: new[] { "glúten", "lactose" });

        product.StationId.Should().Be(stationId);
        product.Allergens.Should().BeEquivalentTo("glúten", "lactose");
    }

    [Fact]
    public void UpdateDetails_Com_Nome_Vazio_Lanca_DomainException()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Mussarela");

        var act = () => product.UpdateDetails(
            name: "  ",
            categoryId: CategoryId,
            stationId: null,
            description: null,
            ingredientsText: null,
            allergens: null,
            allowsFractions: false,
            maxFractions: 1,
            sortOrder: 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateDetails_Com_MaxFractions_Menor_Que_Um_Lanca_DomainException()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Mussarela");

        var act = () => product.UpdateDetails(
            name: "Pizza Mussarela",
            categoryId: CategoryId,
            stationId: null,
            description: null,
            ingredientsText: null,
            allergens: null,
            allowsFractions: true,
            maxFractions: 0,
            sortOrder: 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateDetails_Atualiza_Todos_Os_Campos_De_Cadastro()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Mussarela");
        var newCategoryId = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        product.UpdateDetails(
            name: "Pizza Mussarela Especial",
            categoryId: newCategoryId,
            stationId: stationId,
            description: "Descrição nova",
            ingredientsText: "molho, mussarela, orégano",
            allergens: new[] { "glúten" },
            allowsFractions: true,
            maxFractions: 2,
            sortOrder: 3);

        product.Name.Should().Be("Pizza Mussarela Especial");
        product.CategoryId.Should().Be(newCategoryId);
        product.StationId.Should().Be(stationId);
        product.Description.Should().Be("Descrição nova");
        product.IngredientsText.Should().Be("molho, mussarela, orégano");
        product.Allergens.Should().BeEquivalentTo("glúten");
        product.AllowsFractions.Should().BeTrue();
        product.MaxFractions.Should().Be((short)2);
        product.SortOrder.Should().Be((short)3);
    }

    [Fact]
    public void UpdateSortOrder_Reposiciona_O_Produto()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Mussarela");

        product.UpdateSortOrder(7);

        product.SortOrder.Should().Be((short)7);
    }

    [Fact]
    public void Deactivate_E_Activate_Alternam_IsActive_Sem_Apagar_O_Produto()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Mussarela");

        product.Deactivate();
        product.IsActive.Should().BeFalse();
        product.DeletedAt.Should().BeNull("desativação nunca é exclusão física — US-010 §4, cenário 'Desativação de produto'");

        product.Activate();
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_Marca_DeletedAt_Sem_Remover_Fisicamente()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Mussarela");

        product.SoftDelete();

        product.DeletedAt.Should().NotBeNull();
    }

    /// <summary>
    /// US-013 (Pizza meio a meio) — primeiro consumidor real de <c>FractionGroup</c>. Nenhuma US
    /// anterior (US-010/US-011) expôs escrita deste campo; <see cref="Product.SetFractionGroup"/>
    /// é o método dedicado introduzido para esta história.
    /// </summary>
    [Fact]
    public void SetFractionGroup_Define_O_Grupo_E_Aceita_Nulo_Para_Remover()
    {
        var product = Product.Create(TenantId, CategoryId, "Pizza Mussarela", allowsFractions: true, maxFractions: 4);

        product.SetFractionGroup("PIZZA");
        product.FractionGroup.Should().Be("PIZZA");

        product.SetFractionGroup(null);
        product.FractionGroup.Should().BeNull();
    }
}
