using Nexora.Application.Cashier.Support;
using Nexora.Domain.Cashier;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Cashier;

/// <summary>
/// US-055 §12 (Unitário: "Cálculo do valor esperado com todas as parcelas") — cobre o cenário
/// Gherkin "Composição do valor esperado" (US-055 §4): fundo R$ 200,00 + R$ 1.500,00 em dinheiro +
/// R$ 300,00 de suprimento − R$ 150,00 de sangria = R$ 1.850,00. Função pura, sem banco.
/// </summary>
public sealed class CashExpectedAmountCalculatorTests
{
    [Fact]
    public void Calculate_Soma_Abertura_Pagamentos_Suprimentos_E_Sangrias_Do_Cenario_Gherkin()
    {
        var cashPayments = new[] { new CashPaymentAmounts(1500m, 0m) };
        var movements = new[]
        {
            new CashMovementAmount(CashMovementType.Supply, 300m),
            new CashMovementAmount(CashMovementType.Withdrawal, 150m),
        };

        var result = CashExpectedAmountCalculator.Calculate(200m, cashPayments, movements);

        result.Opening.Should().Be(200m);
        result.CashPayments.Should().Be(1500m);
        result.Supplies.Should().Be(300m);
        result.Withdrawals.Should().Be(-150m, "sangria já carrega o sinal negativo no contrato (US-055 §7)");
        result.Total.Should().Be(1850m);
    }

    [Fact]
    public void Calculate_Desconta_Troco_Do_Dinheiro_Recebido()
    {
        var cashPayments = new[] { new CashPaymentAmounts(100m, 20m) };

        var result = CashExpectedAmountCalculator.Calculate(0m, cashPayments, Array.Empty<CashMovementAmount>());

        result.CashPayments.Should().Be(80m, "documento 04: expected = ... + SUM(amount) - SUM(change_amount) ...");
        result.Total.Should().Be(80m);
    }

    [Fact]
    public void Calculate_Sem_Nenhuma_Parcela_Devolve_Apenas_A_Abertura()
    {
        var result = CashExpectedAmountCalculator.Calculate(
            200m, Array.Empty<CashPaymentAmounts>(), Array.Empty<CashMovementAmount>());

        result.Total.Should().Be(200m);
        result.CashPayments.Should().Be(0m);
        result.Supplies.Should().Be(0m);
        result.Withdrawals.Should().Be(0m);
    }

    [Fact]
    public void Calculate_Varios_Movimentos_Do_Mesmo_Tipo_Sao_Somados()
    {
        var movements = new[]
        {
            new CashMovementAmount(CashMovementType.Withdrawal, 50m),
            new CashMovementAmount(CashMovementType.Withdrawal, 30m),
            new CashMovementAmount(CashMovementType.Supply, 10m),
        };

        var result = CashExpectedAmountCalculator.Calculate(0m, Array.Empty<CashPaymentAmounts>(), movements);

        result.Withdrawals.Should().Be(-80m);
        result.Supplies.Should().Be(10m);
        result.Total.Should().Be(-70m);
    }
}
