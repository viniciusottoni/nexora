using Nexora.Domain.Common;
using Nexora.Domain.Provisioning;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Provisioning;

/// <summary>
/// US-142 — <see cref="BusinessTemplate"/> é o dado que substitui o antigo catálogo estático de
/// código (<see cref="ProvisioningTemplates"/>): cobre a fábrica, a normalização de código e o
/// incremento de versão em <see cref="BusinessTemplate.Update"/> (cenário Gherkin "Atualização de
/// modelo" — a versão nova é o único jeito de um tenant futuro perceber a mudança; tenants já
/// provisionados guardam a versão antiga em <c>tenant_config.template_version</c>).
/// </summary>
public sealed class BusinessTemplateTests
{
    [Fact]
    public void Create_Normaliza_Codigo_Para_Maiusculo_E_Comeca_Na_Versao_Um()
    {
        var template = BusinessTemplate.Create("hamburgueria", "Hamburgueria", "{}", "{}");

        template.Code.Should().Be("HAMBURGUERIA");
        template.Version.Should().Be(1);
        template.IsActive.Should().BeTrue();
        template.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("", "Nome")]
    [InlineData("   ", "Nome")]
    public void Create_Com_Codigo_Vazio_Lanca_DomainException(string code, string name)
    {
        var act = () => BusinessTemplate.Create(code, name, "{}", "{}");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_Com_Nome_Vazio_Lanca_DomainException()
    {
        var act = () => BusinessTemplate.Create("LANCHONETE", "", "{}", "{}");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_Incrementa_Versao_E_Substitui_Conteudo()
    {
        var template = BusinessTemplate.Create("RESTAURANTE", "Restaurante", "{\"a\":1}", "{\"b\":2}");

        template.Update("Restaurante à la carte", "{\"a\":9}", "{\"b\":9}");

        template.Version.Should().Be(2);
        template.Name.Should().Be("Restaurante à la carte");
        template.ConfigJson.Should().Be("{\"a\":9}");
        template.SeedsJson.Should().Be("{\"b\":9}");
    }

    [Fact]
    public void Update_Sucessivo_Continua_Incrementando_A_Versao()
    {
        var template = BusinessTemplate.Create("LANCHONETE", "Lanchonete", "{}", "{}");

        template.Update("Lanchonete", "{\"x\":1}", "{}");
        template.Update("Lanchonete", "{\"x\":2}", "{}");

        template.Version.Should().Be(3);
    }

    [Fact]
    public void Update_Com_Nome_Vazio_Lanca_DomainException_E_Nao_Altera_Versao()
    {
        var template = BusinessTemplate.Create("LANCHONETE", "Lanchonete", "{}", "{}");

        var act = () => template.Update("", "{}", "{}");

        act.Should().Throw<DomainException>();
        template.Version.Should().Be(1);
    }
}
