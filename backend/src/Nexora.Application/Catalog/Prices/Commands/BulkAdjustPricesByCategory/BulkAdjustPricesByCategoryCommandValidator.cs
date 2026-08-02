using FluentValidation;

namespace Nexora.Application.Catalog.Prices.Commands.BulkAdjustPricesByCategory;

public sealed class BulkAdjustPricesByCategoryCommandValidator : AbstractValidator<BulkAdjustPricesByCategoryCommand>
{
    public BulkAdjustPricesByCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Selecione uma categoria.");

        RuleFor(x => x.Channel)
            .NotEmpty().WithMessage("Selecione um canal.");

        RuleFor(x => x.Percent)
            // -100% zera o preço (permitido pelo domínio, Price.Create aceita valor zero); abaixo
            // disso o resultado seria sempre negativo para qualquer preço positivo, então é
            // recusado aqui mesmo antes de tocar o banco.
            .GreaterThanOrEqualTo(-100m).WithMessage("O reajuste não pode reduzir o preço abaixo de zero.")
            .PrecisionScale(6, 3, ignoreTrailingZeros: true)
            .WithMessage("O percentual deve ter no máximo 3 inteiros e 3 casas decimais.");
    }
}
