using FluentValidation;

namespace Nexora.Application.Catalog.ModifierGroups.Commands.DeleteModifierGroup;

public sealed class DeleteModifierGroupCommandValidator : AbstractValidator<DeleteModifierGroupCommand>
{
    public DeleteModifierGroupCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty().WithMessage("O grupo de modificadores é obrigatório.");
    }
}
