using FluentValidation;

namespace Nexora.Application.Areas.Commands.CreateArea;

public sealed class CreateAreaCommandValidator : AbstractValidator<CreateAreaCommand>
{
    public CreateAreaCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Informe o nome do ambiente.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");
    }
}
