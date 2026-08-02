using FluentValidation;

namespace Nexora.Application.Catalog.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Informe um nome.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("A descrição deve ter no máximo 500 caracteres.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Position)
            .GreaterThanOrEqualTo((short)0).WithMessage("A posição não pode ser negativa.")
            .When(x => x.Position.HasValue);
    }
}
