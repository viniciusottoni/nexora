using FluentValidation;

namespace Nexora.Application.Catalog.Modifiers.Commands.MarkModifierUnavailable;

public sealed class MarkModifierUnavailableCommandValidator : AbstractValidator<MarkModifierUnavailableCommand>
{
    public MarkModifierUnavailableCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty().WithMessage("O grupo de modificadores é obrigatório.");
        RuleFor(x => x.ModifierId).NotEmpty().WithMessage("O modificador é obrigatório.");
    }
}
