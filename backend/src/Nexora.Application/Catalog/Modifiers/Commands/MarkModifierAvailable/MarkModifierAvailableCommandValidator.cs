using FluentValidation;

namespace Nexora.Application.Catalog.Modifiers.Commands.MarkModifierAvailable;

public sealed class MarkModifierAvailableCommandValidator : AbstractValidator<MarkModifierAvailableCommand>
{
    public MarkModifierAvailableCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty().WithMessage("O grupo de modificadores é obrigatório.");
        RuleFor(x => x.ModifierId).NotEmpty().WithMessage("O modificador é obrigatório.");
    }
}
