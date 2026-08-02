using Nexora.Domain.Catalog;
using FluentValidation;

namespace Nexora.Application.Catalog.Prices.Commands.SetVariantPrice;

public sealed class SetVariantPriceCommandValidator : AbstractValidator<SetVariantPriceCommand>
{
    public SetVariantPriceCommandValidator()
    {
        RuleFor(x => x.VariantId)
            .NotEmpty().WithMessage("Selecione uma variante.");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0m).WithMessage("O preço não pode ser negativo.")
            .PrecisionScale(12, 2, ignoreTrailingZeros: true)
            .WithMessage("O preço deve ter no máximo 10 inteiros e 2 casas decimais.");

        RuleFor(x => x.Channel)
            .Must(c => Enum.TryParse<Channel>(c, ignoreCase: true, out _))
            .WithMessage("Canal inválido.")
            .When(x => x.Channel is not null);
    }
}
