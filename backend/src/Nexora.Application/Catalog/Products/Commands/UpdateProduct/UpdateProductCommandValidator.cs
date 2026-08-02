using FluentValidation;

namespace Nexora.Application.Catalog.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Informe um nome.")
            .MaximumLength(150).WithMessage("O nome deve ter no máximo 150 caracteres.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("A descrição deve ter no máximo 1000 caracteres.")
            .When(x => x.Description is not null);

        RuleFor(x => x.IngredientsText)
            .MaximumLength(1000).WithMessage("Os ingredientes devem ter no máximo 1000 caracteres.")
            .When(x => x.IngredientsText is not null);

        RuleFor(x => x.MaxFractions)
            .GreaterThanOrEqualTo((short)1).WithMessage("A quantidade máxima de frações precisa ser pelo menos 1.")
            .When(x => x.MaxFractions.HasValue);

        RuleFor(x => x.Position)
            .GreaterThanOrEqualTo((short)0).WithMessage("A posição não pode ser negativa.")
            .When(x => x.Position.HasValue);
    }
}
