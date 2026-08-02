using Nexora.Domain.Catalog;
using Nexora.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Catalog;

/// <summary>
/// US-011 (Variações de produto com preço próprio) §12 ("Unitário: criação de variação implícita
/// para produto sem tamanho" / "validação de compatibilidade por size_code") — cobre as
/// invariantes de <see cref="ProductVariant"/> isoladas de banco/HTTP. A criação automática da
/// variação implícita ao cadastrar um produto sem variação (US-011 §3.1) é coberta em
/// <c>Nexora.IntegrationTests.CatalogIntegrationTests</c>, junto com o fluxo completo de preço.
/// <see cref="ProductVariant.UpdatePrepTimeThresholds"/> é escopo da US-016 e não é coberto aqui.
/// </summary>
public sealed class ProductVariantTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    [Fact]
    public void Create_Com_Nome_Vazio_Lanca_DomainException()
    {
        var act = () => ProductVariant.Create(TenantId, ProductId, name: "   ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Com_PrepMinutes_Negativo_Lanca_DomainException()
    {
        var act = () => ProductVariant.Create(TenantId, ProductId, "Grande", prepMinutes: -1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Preenche_Campos_Padrao_E_Fica_Ativa_Por_Padrao()
    {
        var variant = ProductVariant.Create(TenantId, ProductId, "Grande", sizeCode: "G", sku: "PZ-G");

        variant.Name.Should().Be("Grande");
        variant.SizeCode.Should().Be("G");
        variant.Sku.Should().Be("PZ-G");
        variant.PrepMinutes.Should().Be((short)10, "10 minutos é o valor padrão quando não informado");
        variant.IsDefault.Should().BeFalse();
        variant.IsActive.Should().BeTrue();
        variant.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Create_Aceita_IsDefault_Verdadeiro_Para_A_Variacao_Unica_Implicita()
    {
        var variant = ProductVariant.Create(TenantId, ProductId, "Único", isDefault: true);

        variant.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetails_Com_Nome_Vazio_Lanca_DomainException()
    {
        var variant = ProductVariant.Create(TenantId, ProductId, "Grande");

        var act = () => variant.UpdateDetails(name: "  ", sku: null, sizeCode: null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateDetails_Atualiza_Nome_Sku_E_SizeCode_Sem_Tocar_PrepMinutes()
    {
        var variant = ProductVariant.Create(TenantId, ProductId, "Grande", prepMinutes: 15, sizeCode: "G");

        variant.UpdateDetails("Família", "PZ-F", "F");

        variant.Name.Should().Be("Família");
        variant.Sku.Should().Be("PZ-F");
        variant.SizeCode.Should().Be("F");
        variant.PrepMinutes.Should().Be((short)15, "UpdateDetails (US-011) não é responsável por tempo de preparo/limiares — isso é escopo de UpdatePrepTimeThresholds (US-016)");
    }

    [Fact]
    public void MarkAsDefault_E_UnmarkAsDefault_Alternam_IsDefault()
    {
        var variant = ProductVariant.Create(TenantId, ProductId, "Grande");

        variant.MarkAsDefault();
        variant.IsDefault.Should().BeTrue();

        variant.UnmarkAsDefault();
        variant.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_E_Activate_Alternam_IsActive_Sem_Apagar_A_Variante()
    {
        var variant = ProductVariant.Create(TenantId, ProductId, "Grande");

        variant.Deactivate();
        variant.IsActive.Should().BeFalse();
        variant.DeletedAt.Should().BeNull("desativação nunca é exclusão física — mesma regra de Product/Category, US-011 §3.1");

        variant.Activate();
        variant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_Marca_DeletedAt_Sem_Remover_Fisicamente()
    {
        var variant = ProductVariant.Create(TenantId, ProductId, "Grande");

        variant.SoftDelete();

        variant.DeletedAt.Should().NotBeNull();
    }
}
