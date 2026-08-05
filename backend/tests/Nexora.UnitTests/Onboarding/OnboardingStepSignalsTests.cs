using Nexora.Application.Onboarding.Support;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Onboarding;

/// <summary>
/// US-141 §3.1 — cobre o sinal puro "este campo JSONB de <c>TenantConfig</c> saiu do valor-padrão",
/// usado pelo recálculo dos passos <c>BRANDING</c>/<c>PAYMENT_CONFIG</c>. Sem banco (a leitura de
/// <c>TenantConfig</c> em si é coberta por <c>Nexora.IntegrationTests.OnboardingIntegrationTests</c>).
/// </summary>
public sealed class OnboardingStepSignalsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void HasNonDefaultJson_Falso_Para_Ausente_Ou_Vazio_Ou_Default(string? json)
    {
        OnboardingStepSignals.HasNonDefaultJson(json).Should().BeFalse();
    }

    [Fact]
    public void HasNonDefaultJson_Verdadeiro_Para_Objeto_Com_Conteudo()
    {
        OnboardingStepSignals.HasNonDefaultJson("""{"primaryColor":"#FF0000"}""").Should().BeTrue();
    }

    [Fact]
    public void HasNonDefaultJson_Verdadeiro_Para_Array_Com_Conteudo()
    {
        OnboardingStepSignals.HasNonDefaultJson("""["PIX","CREDIT_CARD"]""").Should().BeTrue();
    }

    [Fact]
    public void HasNonDefaultJson_Verdadeiro_Para_Escalar_Nao_Vazio()
    {
        // JSON tecnicamente válido fora do formato objeto/array esperado — tratado como "tem
        // conteúdo" em vez de lançar, mesmo espírito defensivo de BusinessDayPolicy.
        OnboardingStepSignals.HasNonDefaultJson("\"configurado\"").Should().BeTrue();
    }

    [Fact]
    public void HasNonDefaultJson_Falso_Para_Json_Malformado()
    {
        OnboardingStepSignals.HasNonDefaultJson("{not-json").Should().BeFalse();
    }
}
