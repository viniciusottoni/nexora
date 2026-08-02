using Nexora.Domain.Catalog;
using Nexora.Domain.Common;
using Nexora.Application.Catalog.PrepTime;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Catalog;

/// <summary>
/// US-016 §12 ("Unitário: herança de limiar do tenant quando o produto não define o próprio" —
/// a herança em si é testada em Application/IntegrationTests, aqui é a regra do Domain: as
/// invariantes de ordenação de <see cref="ProductVariant.UpdatePrepTimeThresholds"/>) isoladas
/// de banco/HTTP.
/// </summary>
public sealed class PrepTimeThresholdsTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    private static ProductVariant CreateVariant() =>
        ProductVariant.Create(TenantId, ProductId, "Grande");

    [Fact]
    public void Resolve_Limiares_Do_Tenant_Usa_As_Chaves_Gravadas_Pelo_Template_Pizzeria()
    {
        var thresholds = """{"orderWarnMinutes":12,"orderCriticalMinutes":18}""";

        var result = TenantPrepTimeDefaults.Resolve(thresholds);

        result.WarnMinutes.Should().Be(12);
        result.CriticalMinutes.Should().Be(18);
    }

    [Fact]
    public void UpdatePrepTimeThresholds_Com_Valores_Validos_Atualiza_Os_Tres_Campos()
    {
        var variant = CreateVariant();

        variant.UpdatePrepTimeThresholds(12, 15, 20);

        variant.PrepMinutes.Should().Be(12);
        variant.WarnMinutes.Should().Be(15);
        variant.CriticalMinutes.Should().Be(20);
    }

    /// <summary>Cenário Gherkin "Limiar específico do produto": nulo é uma combinação válida (herda do tenant).</summary>
    [Fact]
    public void UpdatePrepTimeThresholds_Com_Limiares_Nulos_E_Valido()
    {
        var variant = CreateVariant();

        var act = () => variant.UpdatePrepTimeThresholds(10, null, null);

        act.Should().NotThrow();
        variant.WarnMinutes.Should().BeNull();
        variant.CriticalMinutes.Should().BeNull();
    }

    [Fact]
    public void UpdatePrepTimeThresholds_Com_Preparo_Negativo_Lanca_DomainException()
    {
        var variant = CreateVariant();

        var act = () => variant.UpdatePrepTimeThresholds(-1, null, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdatePrepTimeThresholds_Com_Atencao_Menor_Que_Preparo_Lanca_DomainException()
    {
        var variant = CreateVariant();

        var act = () => variant.UpdatePrepTimeThresholds(12, 10, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdatePrepTimeThresholds_Com_Critico_Menor_Que_Atencao_Lanca_DomainException()
    {
        var variant = CreateVariant();

        var act = () => variant.UpdatePrepTimeThresholds(10, 15, 12);

        act.Should().Throw<DomainException>();
    }

    /// <summary>Sem limiar de atenção próprio, o crítico é validado contra o próprio preparo.</summary>
    [Fact]
    public void UpdatePrepTimeThresholds_Com_Critico_Menor_Que_Preparo_Sem_Atencao_Lanca_DomainException()
    {
        var variant = CreateVariant();

        var act = () => variant.UpdatePrepTimeThresholds(12, null, 10);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdatePrepTimeThresholds_Com_Critico_Igual_Ao_Preparo_Sem_Atencao_E_Valido()
    {
        var variant = CreateVariant();

        var act = () => variant.UpdatePrepTimeThresholds(12, null, 12);

        act.Should().NotThrow();
        variant.CriticalMinutes.Should().Be(12);
    }

    [Fact]
    public void UpdatePrepTimeThresholds_Atualiza_UpdatedAt()
    {
        var variant = CreateVariant();
        var before = variant.UpdatedAt;

        variant.UpdatePrepTimeThresholds(12, 15, 20);

        variant.UpdatedAt.Should().BeOnOrAfter(before);
    }
}
