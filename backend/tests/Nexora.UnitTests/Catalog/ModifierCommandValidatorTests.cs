using FluentAssertions;
using Nexora.Application.Catalog.Modifiers.Commands.CreateModifier;
using Nexora.Application.Catalog.Modifiers.Commands.UpdateModifier;
using Xunit;

namespace Nexora.UnitTests.Catalog;

public sealed class ModifierCommandValidatorTests
{
    [Theory]
    [InlineData("1.001")]
    [InlineData("10000000000.00")]
    public void Create_Rejeita_Preco_Fora_De_MoneyAmount(string rawPrice)
    {
        var command = new CreateModifierCommand(
            Guid.NewGuid(), "Borda", decimal.Parse(rawPrice, System.Globalization.CultureInfo.InvariantCulture),
            IngredientId: null, Quantity: null, SortOrder: 0);

        new CreateModifierCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("1.001")]
    [InlineData("10000000000.00")]
    public void Update_Rejeita_Preco_Fora_De_MoneyAmount(string rawPrice)
    {
        var command = new UpdateModifierCommand(
            Guid.NewGuid(), Guid.NewGuid(), decimal.Parse(rawPrice, System.Globalization.CultureInfo.InvariantCulture));

        new UpdateModifierCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Create_Aceita_Quantidade_Com_Quatro_Casas()
    {
        var command = new CreateModifierCommand(
            Guid.NewGuid(), "Borda", 5.50m, IngredientId: Guid.NewGuid(), Quantity: 0.1234m, SortOrder: 0);

        new CreateModifierCommandValidator().Validate(command).IsValid.Should().BeTrue();
    }
}
