using FluentValidation;
using Nexora.Application.Catalog.Variants;
using Nexora.Domain.Catalog;

namespace Nexora.Application.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Channel)
            .Must(channel => ChannelParser.TryParse(channel, out _))
            .WithMessage("Canal de venda inválido.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("O pedido precisa ter pelo menos um item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.VariantId).NotEmpty().WithMessage("Selecione o item do cardápio.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan((short)0).WithMessage("A quantidade precisa ser pelo menos 1.")
                .LessThanOrEqualTo((short)99).WithMessage("Quantidade inválida.");

            item.RuleForEach(i => i.Modifiers).ChildRules(modifier =>
            {
                modifier.RuleFor(m => m.ModifierId).NotEmpty().WithMessage("Modificador inválido.");
                modifier.RuleFor(m => m.Quantity).GreaterThan((short)0).WithMessage("Quantidade do modificador precisa ser pelo menos 1.");
            });

            item.RuleForEach(i => i.Fractions).ChildRules(fraction =>
            {
                fraction.RuleFor(f => f.VariantId).NotEmpty().WithMessage("Variante da fração inválida.");
                fraction.RuleFor(f => f.Weight).GreaterThan(0m).WithMessage("O peso da fração precisa ser maior que zero.")
                    .LessThanOrEqualTo(1m).WithMessage("O peso da fração não pode passar de 1.");
            });

            // Docs/Domain/03-Operacao.md, regra #1: a soma dos weight de um item deve ser
            // exatamente 1 (mesma regra de AddOrderItemCommandValidator, aplicada por ITEM aqui —
            // um pedido com vários itens meio a meio precisa que CADA item feche em 1, não a soma
            // de todos os itens do pedido).
            item.RuleFor(i => i.Fractions)
                .Must(fractions => fractions is null or { Count: 0 } || Math.Round(fractions.Sum(f => f.Weight), 4) == 1.0000m)
                .WithMessage("A soma dos pesos das frações de um item precisa ser exatamente 1 (ex.: duas metades de 0,5).")
                .When(i => i.Fractions is { Count: > 0 });
        });
    }
}
