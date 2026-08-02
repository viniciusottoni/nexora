using FluentValidation;

namespace Nexora.Application.Catalog.ModifierGroups.Commands.CreateModifierGroup;

public sealed class CreateModifierGroupCommandValidator : AbstractValidator<CreateModifierGroupCommand>
{
    public CreateModifierGroupCommandValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Informe um nome para o grupo de modificadores.")
            .Must(value => value.Trim().Length <= 100).WithMessage("Nome do grupo deve ter até 100 caracteres.");

        RuleFor(x => x.MinSelect)
            .GreaterThanOrEqualTo((short)0).WithMessage("A quantidade mínima de seleção não pode ser negativa.");

        RuleFor(x => x.MinSelect)
            .GreaterThanOrEqualTo((short)1)
            .When(x => x.IsRequired)
            .WithMessage("Um grupo obrigatório precisa exigir ao menos uma seleção.");

        RuleFor(x => x.MaxSelect)
            .GreaterThanOrEqualTo(x => x.MinSelect).WithMessage("A quantidade máxima de seleção não pode ser menor que a mínima.");

        RuleFor(x => x.MaxSelect)
            .LessThanOrEqualTo((short)100).WithMessage("A quantidade máxima de seleção deve ser até 100.");
    }
}
