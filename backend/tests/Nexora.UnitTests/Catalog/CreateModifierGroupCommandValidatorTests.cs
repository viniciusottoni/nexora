using FluentAssertions;
using Nexora.Application.Catalog.ModifierGroups.Commands.CreateModifierGroup;
using Xunit;

namespace Nexora.UnitTests.Catalog;

public sealed class CreateModifierGroupCommandValidatorTests
{
    [Fact]
    public void Validate_Grupo_Obrigatorio_Com_Minimo_Zero_E_Invalido()
    {
        var command = new CreateModifierGroupCommand("Tamanho", 0, 1, true, 0);

        var result = new CreateModifierGroupCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateModifierGroupCommand.MinSelect)
            && error.ErrorMessage.Contains("grupo obrigatório"));
    }
}
