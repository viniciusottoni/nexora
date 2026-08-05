using Nexora.Application.Provisioning;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Provisioning;

/// <summary>
/// US-142 §4, cenário "Aplicação do modelo": "praças, categorias e limiares devem refletir o
/// modelo E devem ser diferentes dos de uma pizzaria". Trava o conteúdo dos 3 modelos novos contra
/// esse critério — sem este teste, seria fácil "clonar" a pizzaria com o nome trocado (o que passa
/// numa checagem rasa de "são objetos diferentes" mas falha o espírito do cenário).
/// </summary>
public sealed class BusinessTemplateSeedCatalogTests
{
    [Fact]
    public void Catalogo_Tem_Os_Quatro_Codigos_Esperados()
    {
        var codes = BusinessTemplateSeedCatalog.All().Select(t => t.Code);

        codes.Should().BeEquivalentTo("PIZZERIA", "HAMBURGUERIA", "RESTAURANTE", "LANCHONETE");
    }

    [Fact]
    public void Cada_Modelo_Tem_Seu_Proprio_Recurso_Gargalo_Diferente_Da_Pizzaria()
    {
        var pizzeria = BusinessTemplateSeedCatalog.Pizzeria();
        var hamburgueria = BusinessTemplateSeedCatalog.Hamburgueria();
        var restaurante = BusinessTemplateSeedCatalog.Restaurante();
        var lanchonete = BusinessTemplateSeedCatalog.Lanchonete();

        BusinessTemplateDataMapper.ExtractBottleneckResource(pizzeria.Config.Operation).Should().Be("OVEN");
        BusinessTemplateDataMapper.ExtractBottleneckResource(hamburgueria.Config.Operation).Should().Be("GRILL");
        BusinessTemplateDataMapper.ExtractBottleneckResource(restaurante.Config.Operation).Should().Be("ASSEMBLY");
        BusinessTemplateDataMapper.ExtractBottleneckResource(lanchonete.Config.Operation).Should().Be("GRILL");

        // O gargalo declarado precisa existir de fato entre as praças do próprio modelo — senão
        // ProvisionTenantCommandHandler não teria nenhuma praça para marcar como gargalo.
        foreach (var template in new[] { pizzeria, hamburgueria, restaurante, lanchonete })
        {
            var resource = BusinessTemplateDataMapper.ExtractBottleneckResource(template.Config.Operation);
            template.Stations.Select(s => s.Type.ToString().ToUpperInvariant())
                .Should().Contain(resource!.ToUpperInvariant());
        }
    }

    [Fact]
    public void Numero_De_Pracas_Difere_Entre_Modelos()
    {
        BusinessTemplateSeedCatalog.Pizzeria().Stations.Should().HaveCount(5);
        BusinessTemplateSeedCatalog.Hamburgueria().Stations.Should().HaveCount(5);
        BusinessTemplateSeedCatalog.Restaurante().Stations.Should().HaveCount(5);
        BusinessTemplateSeedCatalog.Lanchonete().Stations.Should().HaveCount(4); // sem sobremesa dedicada

        // Mesmo com a mesma contagem, os NOMES/tipos de praça de cada modelo são próprios (não a
        // pizzaria com o nome trocado).
        BusinessTemplateSeedCatalog.Hamburgueria().Stations.Select(s => s.Name)
            .Should().NotBeEquivalentTo(BusinessTemplateSeedCatalog.Pizzeria().Stations.Select(s => s.Name));
        BusinessTemplateSeedCatalog.Restaurante().Stations.Select(s => s.Name)
            .Should().NotBeEquivalentTo(BusinessTemplateSeedCatalog.Pizzeria().Stations.Select(s => s.Name));
    }

    [Fact]
    public void Numero_De_Categorias_De_Despesa_Difere_Entre_Modelos()
    {
        BusinessTemplateSeedCatalog.Pizzeria().ExpenseCategories.Should().HaveCount(15);
        BusinessTemplateSeedCatalog.Hamburgueria().ExpenseCategories.Should().HaveCount(16);
        BusinessTemplateSeedCatalog.Restaurante().ExpenseCategories.Should().HaveCount(17);
        BusinessTemplateSeedCatalog.Lanchonete().ExpenseCategories.Should().HaveCount(12);
    }

    [Fact]
    public void Numero_De_Contas_Financeiras_Difere_Entre_Modelos()
    {
        BusinessTemplateSeedCatalog.Pizzeria().FinancialAccounts.Should().HaveCount(4);
        BusinessTemplateSeedCatalog.Hamburgueria().FinancialAccounts.Should().HaveCount(4);
        BusinessTemplateSeedCatalog.Restaurante().FinancialAccounts.Should().HaveCount(5);
        BusinessTemplateSeedCatalog.Lanchonete().FinancialAccounts.Should().HaveCount(3);
    }

    /// <summary>Docs/domain/12 §3 — os limiares/operação declarados na tabela "Outros modelos de negócio" precisam bater com o catálogo.</summary>
    [Theory]
    [InlineData("PIZZERIA", 2, 5, "OVEN")]
    [InlineData("HAMBURGUERIA", 1, 5, "GRILL")]
    [InlineData("RESTAURANTE", 1, 4, "ASSEMBLY")]
    public void MaxFractions_BusinessDayStartHour_E_Bottleneck_Batem_Com_A_Doc_De_Seeds(
        string code, int expectedMaxFractions, int expectedBusinessDayStartHour, string expectedBottleneck)
    {
        var template = BusinessTemplateSeedCatalog.All().Single(t => t.Code == code).Template;

        template.Config.Operation["maxFractions"].Should().Be(expectedMaxFractions);
        template.Config.Operation["businessDayStartHour"].Should().Be(expectedBusinessDayStartHour);
        BusinessTemplateDataMapper.ExtractBottleneckResource(template.Config.Operation).Should().Be(expectedBottleneck);
    }

    [Fact]
    public void Papeis_De_Sistema_Sao_Os_Mesmos_Sete_Em_Todos_Os_Modelos()
    {
        var expectedCodes = new[] { "OWNER", "MANAGER", "CASHIER", "WAITER", "KITCHEN", "STOCK", "COURIER" };

        foreach (var (code, _, template) in BusinessTemplateSeedCatalog.All())
        {
            template.Roles.Select(r => r.Code).Should().BeEquivalentTo(expectedCodes, $"modelo {code}");
        }
    }
}
