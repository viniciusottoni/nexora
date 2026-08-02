using FluentValidation;

namespace Nexora.Application.Catalog.Modifiers.Commands.UpdateModifier;

public sealed class UpdateModifierCommandValidator : AbstractValidator<UpdateModifierCommand>
{
    public UpdateModifierCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty().WithMessage("O grupo de modificadores é obrigatório.");
        RuleFor(x => x.ModifierId).NotEmpty().WithMessage("O modificador é obrigatório.");
        RuleFor(x => x.PriceDelta)
            .PrecisionScale(12, 2, ignoreTrailingZeros: false)
            .WithMessage("O preço deve ter no máximo 10 dígitos inteiros e 2 casas decimais.");
    }
}
