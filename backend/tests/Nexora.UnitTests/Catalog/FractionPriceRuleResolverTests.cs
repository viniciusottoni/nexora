using Nexora.Application.Catalog.FractionPricing;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Catalog;

/// <summary>
/// US-013 §5/§8 (RN-009, <b>[HIPÓTESE]</b>) — resolve <c>tenant_config.operation.halfAndHalfPricing</c>.
/// Padrão (maior valor) quando ausente/inválido, já que a regra é hipótese não validada com o
/// cliente.
/// </summary>
public sealed class FractionPriceRuleResolverTests
{
    [Theory]
    [InlineData("""{"halfAndHalfPricing":"HIGHEST"}""", FractionPriceRule.Highest)]
    [InlineData("""{"halfAndHalfPricing":"AVERAGE"}""", FractionPriceRule.Average)]
    [InlineData("""{"halfAndHalfPricing":"PROPORTIONAL"}""", FractionPriceRule.Proportional)]
    [InlineData("""{"halfAndHalfPricing":"average"}""", FractionPriceRule.Average)]
    public void Resolve_Le_A_Regra_Configurada_No_Json_De_Operation(string operationJson, FractionPriceRule expected)
    {
        FractionPriceRuleResolver.Resolve(operationJson).Should().Be(expected);
    }

    [Fact]
    public void Resolve_Sem_Config_Alguma_Cai_No_Padrao_Highest()
    {
        FractionPriceRuleResolver.Resolve(null).Should().Be(FractionPriceRule.Highest);
        FractionPriceRuleResolver.Resolve("").Should().Be(FractionPriceRule.Highest);
        FractionPriceRuleResolver.Resolve("{}").Should().Be(FractionPriceRule.Highest);
    }

    [Fact]
    public void Resolve_Com_Valor_Desconhecido_Cai_No_Padrao()
    {
        FractionPriceRuleResolver.Resolve("""{"halfAndHalfPricing":"MEDIANA"}""").Should().Be(FractionPriceRule.Highest);
    }

    [Fact]
    public void Resolve_Com_Json_Malformado_Nao_Lanca_E_Cai_No_Padrao()
    {
        var act = () => FractionPriceRuleResolver.Resolve("{ isso não é json");

        act.Should().NotThrow();
        FractionPriceRuleResolver.Resolve("{ isso não é json").Should().Be(FractionPriceRule.Highest);
    }

    [Fact]
    public void Resolve_Ignora_Outras_Chaves_Do_Operation_Json()
    {
        FractionPriceRuleResolver.Resolve("""{"businessDayCutoverHour":5,"halfAndHalfPricing":"PROPORTIONAL"}""")
            .Should().Be(FractionPriceRule.Proportional);
    }
}
