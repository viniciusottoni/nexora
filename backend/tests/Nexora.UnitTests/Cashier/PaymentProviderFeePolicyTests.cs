using Nexora.Application.Cashier.Support;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Cashier;

/// <summary>US-058 §12 ("Cálculo de valor líquido por provedor, forma e parcelas").</summary>
public sealed class PaymentProviderFeePolicyTests
{
    private const string PaymentsJson = """
        { "providers": [ { "code": "CIELO", "fees": { "CREDIT": 2.8, "DEBIT": 1.5 } } ] }
        """;

    [Fact]
    public void Resolve_Fee_Percent_Le_A_Taxa_Configurada_Do_Provedor_E_Forma()
    {
        PaymentProviderFeePolicy.ResolveFeePercent(PaymentsJson, "CIELO", "CREDIT").Should().Be(2.8m);
        PaymentProviderFeePolicy.ResolveFeePercent(PaymentsJson, "CIELO", "DEBIT").Should().Be(1.5m);
    }

    [Fact]
    public void Resolve_Fee_Percent_Cai_Em_Zero_Quando_Provedor_Nao_Configurado()
    {
        PaymentProviderFeePolicy.ResolveFeePercent(PaymentsJson, "MERCADO_PAGO", "CREDIT").Should().Be(0m);
    }

    [Fact]
    public void Resolve_Fee_Percent_Cai_Em_Zero_Sem_Provedor()
    {
        PaymentProviderFeePolicy.ResolveFeePercent(PaymentsJson, null, "CREDIT").Should().Be(0m);
    }

    [Fact]
    public void Resolve_Fee_Percent_Cai_Em_Zero_Com_Json_Malformado()
    {
        PaymentProviderFeePolicy.ResolveFeePercent("{ not valid json", "CIELO", "CREDIT").Should().Be(0m);
    }

    [Fact]
    public void Calculate_Fee_Arredonda_Half_Up_Em_Duas_Casas()
    {
        // US-058 §4, cenário "Valor líquido calculado": R$ 100,00 em crédito, taxa de 2,8% -> R$ 2,80 de taxa, R$ 97,20 líquido.
        var fee = PaymentProviderFeePolicy.CalculateFee(100m, 2.8m);
        fee.Should().Be(2.80m);
        (100m - fee).Should().Be(97.20m);
    }
}
