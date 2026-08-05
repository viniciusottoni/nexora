using Nexora.Application.Cashier.Commands.ApplyDiscount;
using Nexora.Application.Cashier.Commands.WaiveSessionServiceFee;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Cashier;

public sealed class CashierCommandValidatorTests
{
    [Fact]
    public void ApplyDiscount_Rejeita_Percentual_E_Valor_Ao_Mesmo_Tempo()
    {
        var command = new ApplyDiscountCommand(
            Guid.NewGuid(), Percent: 10m, Amount: 5m, "ambíguo", "SESSION", OrderItemId: null, AuthorizationToken: null);

        var result = new ApplyDiscountCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Informe apenas o percentual ou o valor do desconto.");
    }

    [Fact]
    public void WaiveSessionServiceFee_Rejeita_Motivo_Vazio()
    {
        var command = new WaiveSessionServiceFeeCommand(Guid.NewGuid(), "", "FULL");

        var result = new WaiveSessionServiceFeeCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("O motivo da retirada da taxa é obrigatório.");
    }
}
