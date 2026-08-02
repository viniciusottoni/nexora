using FluentValidation;

namespace Nexora.Application.Catalog.Prices.Commands.SetVariantChannelPrice;

public sealed class SetVariantChannelPriceCommandValidator : AbstractValidator<SetVariantChannelPriceCommand>
{
    public SetVariantChannelPriceCommandValidator()
    {
        RuleFor(x => x.VariantId)
            .NotEmpty().WithMessage("Selecione uma variante.");

        RuleFor(x => x.Prices)
            .NotEmpty().WithMessage("Informe ao menos um preço por canal.");

        RuleForEach(x => x.Prices).ChildRules(price =>
        {
            price.RuleFor(p => p.Channel)
                .NotEmpty().WithMessage("Canal é obrigatório.");

            price.RuleFor(p => p.Amount)
                .GreaterThanOrEqualTo(0m).WithMessage("O preço não pode ser negativo.")
                .PrecisionScale(12, 2, ignoreTrailingZeros: true)
                .WithMessage("O preço deve ter no máximo 10 inteiros e 2 casas decimais.");
        });
    }
}
