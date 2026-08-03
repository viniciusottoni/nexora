using FluentValidation;

namespace Nexora.Application.Orders.Commands.AddItemToOrder;

public sealed class AddItemToOrderCommandValidator : AbstractValidator<AddItemToOrderCommand>
{
    public AddItemToOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Selecione o pedido.");
        RuleFor(x => x.VariantId).NotEmpty().WithMessage("Selecione o item do cardápio.");

        RuleFor(x => x.Quantity)
            .GreaterThan((short)0).WithMessage("A quantidade precisa ser pelo menos 1.")
            .LessThanOrEqualTo((short)99).WithMessage("Quantidade inválida.");

        RuleForEach(x => x.Modifiers).ChildRules(modifier =>
        {
            modifier.RuleFor(m => m.ModifierId).NotEmpty().WithMessage("Modificador inválido.");
            modifier.RuleFor(m => m.Quantity).GreaterThan((short)0).WithMessage("Quantidade do modificador precisa ser pelo menos 1.");
        });

        RuleForEach(x => x.Fractions).ChildRules(fraction =>
        {
            fraction.RuleFor(f => f.VariantId).NotEmpty().WithMessage("Variante da fração inválida.");
            fraction.RuleFor(f => f.Weight).GreaterThan(0m).WithMessage("O peso da fração precisa ser maior que zero.")
                .LessThanOrEqualTo(1m).WithMessage("O peso da fração não pode passar de 1.");
        });

        RuleFor(x => x.Fractions)
            .Must(fractions => fractions is null or { Count: 0 } || Math.Round(fractions.Sum(f => f.Weight), 4) == 1.0000m)
            .WithMessage("A soma dos pesos das frações precisa ser exatamente 1 (ex.: duas metades de 0,5).")
            .When(x => x.Fractions is { Count: > 0 });
    }
}
