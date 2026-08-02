using FluentValidation;

namespace Nexora.Application.Catalog.ModifierGroups.Commands.UpdateModifierGroup;

public sealed class UpdateModifierGroupCommandValidator : AbstractValidator<UpdateModifierGroupCommand>
{
    public UpdateModifierGroupCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty().WithMessage("O grupo de modificadores é obrigatório.");

        RuleFor(x => x.MinSelect)
            .GreaterThanOrEqualTo((short)0).WithMessage("A quantidade mínima de seleção não pode ser negativa.");

        RuleFor(x => x.MaxSelect)
            .GreaterThanOrEqualTo(x => x.MinSelect).WithMessage("A quantidade máxima de seleção não pode ser menor que a mínima.")
            .LessThanOrEqualTo((short)100).WithMessage("A quantidade máxima de seleção deve ser até 100.");
    }
}
