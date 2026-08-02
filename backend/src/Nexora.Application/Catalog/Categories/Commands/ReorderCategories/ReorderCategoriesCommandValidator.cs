using FluentValidation;

namespace Nexora.Application.Catalog.Categories.Commands.ReorderCategories;

public sealed class ReorderCategoriesCommandValidator : AbstractValidator<ReorderCategoriesCommand>
{
    public ReorderCategoriesCommandValidator()
    {
        RuleFor(x => x.Order)
            .NotEmpty().WithMessage("Informe a nova ordem das categorias.");

        RuleFor(x => x.Order)
            .Must(order => order.Distinct().Count() == order.Count)
            .WithMessage("A ordem não pode repetir a mesma categoria.")
            .When(x => x.Order.Count > 0);
    }
}
