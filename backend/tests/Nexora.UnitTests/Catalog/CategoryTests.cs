using Nexora.Domain.Catalog;
using Nexora.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Catalog;

/// <summary>
/// US-010 (Cadastrar categorias e produtos) §12 ("Unitário: validação de campos obrigatórios e de
/// ordenação") — cobre as invariantes de <see cref="Category"/> isoladas de banco/HTTP. A regra de
/// "não pode repetir a mesma categoria" na reordenação em lote (RN aplicada por
/// <c>ReorderCategoriesCommandHandler</c>) é coberta em
/// <c>Nexora.IntegrationTests.CatalogIntegrationTests</c>.
/// </summary>
public sealed class CategoryTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_Com_Nome_Vazio_Lanca_DomainException()
    {
        var act = () => Category.Create(TenantId, name: "   ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Preenche_Campos_Padrao_E_Fica_Ativa_Por_Padrao()
    {
        var category = Category.Create(TenantId, "Pizzas Salgadas", "Sabores tradicionais", sortOrder: 2);

        category.Name.Should().Be("Pizzas Salgadas");
        category.Description.Should().Be("Sabores tradicionais");
        category.SortOrder.Should().Be((short)2);
        category.IsActive.Should().BeTrue();
        category.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_Com_Nome_Vazio_Lanca_DomainException()
    {
        var category = Category.Create(TenantId, "Pizzas Salgadas");

        var act = () => category.UpdateDetails(name: "", description: null, sortOrder: 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateDetails_Atualiza_Nome_Descricao_E_Posicao()
    {
        var category = Category.Create(TenantId, "Pizzas Salgadas");

        category.UpdateDetails("Pizzas Doces", "Sobremesas em formato de pizza", sortOrder: 5);

        category.Name.Should().Be("Pizzas Doces");
        category.Description.Should().Be("Sobremesas em formato de pizza");
        category.SortOrder.Should().Be((short)5);
    }

    [Fact]
    public void Deactivate_E_Activate_Alternam_IsActive_Sem_Apagar_A_Categoria()
    {
        var category = Category.Create(TenantId, "Bebidas");

        category.Deactivate();
        category.IsActive.Should().BeFalse();
        category.DeletedAt.Should().BeNull("desativação nunca é exclusão física — US-010 §4");

        category.Activate();
        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_Marca_DeletedAt_Sem_Remover_Fisicamente()
    {
        var category = Category.Create(TenantId, "Bebidas");

        category.SoftDelete();

        category.DeletedAt.Should().NotBeNull();
    }
}
