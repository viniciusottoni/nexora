using FluentAssertions;
using Nexora.Application.Catalog.Prices.Commands.BulkAdjustPricesByCategory;
using Nexora.Application.Catalog.Prices.Commands.SetVariantChannelPrice;
using Nexora.Application.Catalog.Prices.Commands.SetVariantPrice;
using Nexora.Contracts.Catalog;
using Xunit;

namespace Nexora.UnitTests.Catalog;

public sealed class PricingCommandValidatorTests
{
    [Theory]
    [InlineData("45.001")]
    [InlineData("10000000000.00")]
    public void SetVariantChannelPrice_Rejeita_Valor_Fora_De_MoneyAmount(string amount)
    {
        var command = new SetVariantChannelPriceCommand(
            Guid.NewGuid(),
            [new ChannelPriceEntry("DineIn", decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture))]);

        var result = new SetVariantChannelPriceCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Prices[0].Amount");
    }

    [Theory]
    [InlineData("45.001")]
    [InlineData("10000000000.00")]
    public void SetVariantPrice_Rejeita_Valor_Fora_De_MoneyAmount(string amount)
    {
        var command = new SetVariantPriceCommand(
            Guid.NewGuid(),
            decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture),
            "DineIn");

        var result = new SetVariantPriceCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(command.Amount));
    }

    [Theory]
    [InlineData("1.0001")]
    [InlineData("1000.000")]
    public void BulkAdjust_Rejeita_Percentual_Fora_De_Numeric_6_3(string percent)
    {
        var command = new BulkAdjustPricesByCategoryCommand(
            Guid.NewGuid(),
            "DineIn",
            decimal.Parse(percent, System.Globalization.CultureInfo.InvariantCulture));

        var result = new BulkAdjustPricesByCategoryCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(command.Percent));
    }
}
