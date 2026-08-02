using FluentAssertions;
using Nexora.Application.Catalog.Variants.Commands.CreateVariant;
using Xunit;

namespace Nexora.UnitTests.Catalog;

public sealed class CreateVariantCommandValidatorTests
{
    [Fact]
    public void Deve_Rejeitar_Preco_Com_Mais_De_Duas_Casas_Decimais()
    {
        var command = new CreateVariantCommand(
            Guid.NewGuid(), "Grande", "G", null, null, false, 45.999m, "DineIn");

        var result = new CreateVariantCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(command.BasePrice));
    }
}
