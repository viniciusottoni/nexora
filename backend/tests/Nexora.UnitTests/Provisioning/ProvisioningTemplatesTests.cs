using Nexora.Domain.Finance;
using Nexora.Domain.Provisioning;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Provisioning;

/// <summary>
/// Cobre a lacuna encontrada na auditoria da US-002: <c>ProvisioningTemplates.BuildPizzeria()</c>
/// não semeava <c>expense_category</c>/<c>financial_account</c> (Docs/Domain/12-Seeds-e-Dados-Iniciais.md
/// §5/§6/§8, passos 7 e 8 do provisionamento automático — RF-PLT-05). Estes testes travam o
/// conteúdo exato do template contra o documento, para que uma regressão futura (alguém editando
/// o template sem olhar o doc) quebre o build em vez de silenciosamente divergir do contrato.
/// </summary>
public sealed class ProvisioningTemplatesTests
{
    [Fact]
    public void IsKnown_Reconhece_Pizzeria_E_Rejeita_Codigo_Desconhecido()
    {
        ProvisioningTemplates.IsKnown(ProvisioningTemplates.Pizzeria).Should().BeTrue();
        ProvisioningTemplates.IsKnown("HAMBURGUERIA").Should().BeFalse();
    }

    [Fact]
    public void Get_Com_Codigo_Desconhecido_Lanca_DomainException()
    {
        var act = () => ProvisioningTemplates.Get("RESTAURANTE");

        act.Should().Throw<Nexora.Domain.Common.DomainException>();
    }

    [Fact]
    public void BuildPizzeria_Semeia_Sete_Papeis_De_Sistema_Incluindo_Owner_Com_Acesso_Total()
    {
        var template = ProvisioningTemplates.Get(ProvisioningTemplates.Pizzeria);

        template.Roles.Should().HaveCount(7);
        template.Roles.Select(r => r.Code).Should().BeEquivalentTo(
            "OWNER", "MANAGER", "CASHIER", "WAITER", "KITCHEN", "STOCK", "COURIER");

        var owner = template.Roles.Single(r => r.Code == "OWNER");
        owner.Permissions.Should().BeEquivalentTo(new[] { "*" });
    }

    [Fact]
    public void BuildPizzeria_Semeia_Cinco_Pracas_De_Producao_Padrao()
    {
        var template = ProvisioningTemplates.Get(ProvisioningTemplates.Pizzeria);

        template.Stations.Should().HaveCount(5);
        template.Stations.Select(s => s.Name).Should().BeEquivalentTo(
            "Montagem", "Forno", "Fritura", "Bar", "Sobremesa");
    }

    /// <summary>Docs/Domain/12 §5 — passo 7 (RF-PLT-05). 15 categorias, com is_cmv correto nas duas primeiras.</summary>
    [Fact]
    public void BuildPizzeria_Semeia_Quinze_Categorias_De_Despesa_Padrao()
    {
        var template = ProvisioningTemplates.Get(ProvisioningTemplates.Pizzeria);

        template.ExpenseCategories.Should().HaveCount(15);
        template.ExpenseCategories.Select(c => c.Name).Should().BeEquivalentTo(new[]
        {
            "Insumos e mercadorias", "Embalagens", "Salários", "Encargos trabalhistas",
            "Aluguel", "Energia elétrica", "Água", "Gás", "Internet e telefonia", "Impostos",
            "Taxas de cartão", "Manutenção", "Marketing", "Contabilidade", "Outras despesas"
        });

        var cmvCategories = template.ExpenseCategories.Where(c => c.IsCmv).Select(c => c.Name);
        cmvCategories.Should().BeEquivalentTo("Insumos e mercadorias", "Embalagens");

        template.ExpenseCategories.Single(c => c.Name == "Salários").Group.Should().Be(ExpenseGroup.Payroll);
        template.ExpenseCategories.Single(c => c.Name == "Impostos").Group.Should().Be(ExpenseGroup.Tax);
    }

    /// <summary>Docs/Domain/12 §6 — passo 8 (RF-PLT-05). 4 contas: caixa, banco e duas adquirentes.</summary>
    [Fact]
    public void BuildPizzeria_Semeia_Quatro_Contas_Financeiras_Padrao()
    {
        var template = ProvisioningTemplates.Get(ProvisioningTemplates.Pizzeria);

        template.FinancialAccounts.Should().HaveCount(4);
        template.FinancialAccounts.Should().BeEquivalentTo(new[]
        {
            new ProvisioningFinancialAccountTemplate("Caixa da loja", "CASH"),
            new ProvisioningFinancialAccountTemplate("Conta bancária", "BANK"),
            new ProvisioningFinancialAccountTemplate("Cielo", "ACQUIRER"),
            new ProvisioningFinancialAccountTemplate("Mercado Pago", "ACQUIRER"),
        });
    }
}
