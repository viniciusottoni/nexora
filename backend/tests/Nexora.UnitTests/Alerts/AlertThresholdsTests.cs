using Nexora.Application.Alerts.Support;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Alerts;

/// <summary>
/// US-080 §4, cenários "Limiar configurável por tenant" e "Limiar padrão do modelo de negócio" —
/// cobre a leitura tipada de <c>TenantConfig.Thresholds</c> (JSONB livre), sem tocar banco.
/// </summary>
public sealed class AlertThresholdsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{}")]
    public void Parse_Sem_Configuracao_Usa_Os_Padroes_Do_Template_Pizzeria(string? json)
    {
        var thresholds = AlertThresholds.Parse(json);

        thresholds.Should().Be(AlertThresholds.Default);
        thresholds.OrderWarnMinutes.Should().Be(12);
        thresholds.OrderCriticalMinutes.Should().Be(18);
        thresholds.CashDivergenceAlert.Should().Be(20.00m);
    }

    [Fact]
    public void Parse_Mescla_Apenas_As_Chaves_Presentes_Sobre_O_Padrao()
    {
        var thresholds = AlertThresholds.Parse("""{"orderCriticalMinutes": 25}""");

        thresholds.OrderCriticalMinutes.Should().Be(25);
        thresholds.OrderWarnMinutes.Should().Be(AlertThresholds.Default.OrderWarnMinutes);
    }

    [Fact]
    public void Parse_Aceita_Valor_Monetario_Como_String_Ou_Numero()
    {
        AlertThresholds.Parse("""{"cashDivergenceAlert": "35.50"}""").CashDivergenceAlert.Should().Be(35.50m);
        AlertThresholds.Parse("""{"cashDivergenceAlert": 35.50}""").CashDivergenceAlert.Should().Be(35.50m);
    }

    [Fact]
    public void Parse_Com_Json_Invalido_Cai_No_Padrao()
    {
        AlertThresholds.Parse("não é json").Should().Be(AlertThresholds.Default);
    }

    [Fact]
    public void ToJson_E_Parse_Sao_Simetricos()
    {
        var original = AlertThresholds.Default with { OrderWarnMinutes = 9, CashDivergenceAlert = 42.10m };

        var roundTripped = AlertThresholds.Parse(original.ToJson());

        roundTripped.Should().Be(original);
    }

    [Fact]
    public void Dois_Tenants_Com_Limiares_Diferentes_Produzem_Valores_Diferentes()
    {
        var tenantA = AlertThresholds.Parse("""{"orderCriticalMinutes": 15}""");
        var tenantB = AlertThresholds.Parse("""{"orderCriticalMinutes": 30}""");

        tenantA.OrderCriticalMinutes.Should().NotBe(tenantB.OrderCriticalMinutes);
    }
}
