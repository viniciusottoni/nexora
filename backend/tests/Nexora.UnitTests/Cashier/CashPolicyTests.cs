using Nexora.Application.Cashier.Support;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Cashier;

/// <summary>
/// US-055 §12 (Unitário: "Avaliação do limiar de justificativa obrigatória") e US-056 §8
/// (<c>operation.maxWithdrawalWithoutAuth</c>) — prova os defaults documentados em
/// <see cref="CashPolicy"/> e a leitura da chave configurada pelo tenant, mesmo espírito de
/// <c>PendingItemsClosePolicyTests</c>/<c>ServiceFeePolicyTests</c>.
/// </summary>
public sealed class CashPolicyTests
{
    [Fact]
    public void ResolveMaxWithdrawalWithoutAuth_Sem_Configuracao_Usa_O_Default_Do_Cenario_Gherkin()
    {
        // US-056 §4, cenário "Sangria acima do limite": "Dado o limite de sangria sem autorização em R$ 300,00".
        CashPolicy.ResolveMaxWithdrawalWithoutAuth(null).Should().Be(300.00m);
        CashPolicy.ResolveMaxWithdrawalWithoutAuth("{}").Should().Be(300.00m);
    }

    [Fact]
    public void ResolveMaxWithdrawalWithoutAuth_Le_A_Chave_Configurada_Pelo_Tenant()
    {
        CashPolicy.ResolveMaxWithdrawalWithoutAuth("""{"maxWithdrawalWithoutAuth": 500.00}""").Should().Be(500.00m);
    }

    [Fact]
    public void ResolveMaxWithdrawalWithoutAuth_Aceita_String_Monetaria_ADR017()
    {
        CashPolicy.ResolveMaxWithdrawalWithoutAuth("""{"maxWithdrawalWithoutAuth": "150.50"}""").Should().Be(150.50m);
    }

    [Fact]
    public void ResolveMaxWithdrawalWithoutAuth_Com_Json_Malformado_Cai_No_Default()
    {
        CashPolicy.ResolveMaxWithdrawalWithoutAuth("{ nao é json").Should().Be(300.00m);
    }

    [Theory]
    [InlineData(6.50, true)] // US-055 §4, cenário "Divergência no fechamento": R$ 6,50 exige justificativa.
    [InlineData(0, false)] // "Fechamento sem divergência": nenhuma divergência não exige nada.
    [InlineData(5.00, false)] // limite exato — não excede o limiar (regra é "acima", estrito).
    [InlineData(5.01, true)]
    public void ResolveDivergenceJustificationThreshold_Avalia_O_Cenario_Gherkin_Corretamente(decimal divergence, bool expectedRequiresJustification)
    {
        var threshold = CashPolicy.ResolveDivergenceJustificationThreshold(null);

        (Math.Abs(divergence) > threshold).Should().Be(expectedRequiresJustification);
    }

    [Fact]
    public void ResolveDivergenceJustificationThreshold_Le_A_Chave_Configurada_Pelo_Tenant()
    {
        CashPolicy.ResolveDivergenceJustificationThreshold("""{"cashDivergenceJustificationThreshold": 10.00}""").Should().Be(10.00m);
    }

    [Fact]
    public void ResolveDivergenceJustificationThreshold_Sem_Configuracao_Usa_O_Default()
    {
        CashPolicy.ResolveDivergenceJustificationThreshold(null).Should().Be(5.00m);
    }
}
