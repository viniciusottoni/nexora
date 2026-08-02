using FluentValidation;

namespace Nexora.Application.Catalog.Products.Commands.ReorderProducts;

public sealed class ReorderProductsCommandValidator : AbstractValidator<ReorderProductsCommand>
{
    public ReorderProductsCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Informe a categoria.");

        RuleFor(x => x.Order)
            .NotEmpty().WithMessage("Informe a nova ordem dos produtos.");

        RuleFor(x => x.Order)
            .Must(order => order.Distinct().Count() == order.Count)
            .WithMessage("A ordem não pode repetir o mesmo produto.")
            .When(x => x.Order.Count > 0);
    }
}
