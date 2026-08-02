using FluentValidation;

namespace Nexora.Application.Catalog.Variants.Commands.UpdateVariant;

public sealed class UpdateVariantCommandValidator : AbstractValidator<UpdateVariantCommand>
{
    public UpdateVariantCommandValidator()
    {
        RuleFor(x => x.VariantId)
            .NotEmpty().WithMessage("Selecione uma variante.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Informe um nome.")
            .MaximumLength(150).WithMessage("O nome deve ter no máximo 150 caracteres.");

        RuleFor(x => x.SizeCode)
            .MaximumLength(16).WithMessage("O código de tamanho deve ter no máximo 16 caracteres.")
            .When(x => x.SizeCode is not null);

        RuleFor(x => x.Sku)
            .MaximumLength(40).WithMessage("O SKU deve ter no máximo 40 caracteres.")
            .When(x => x.Sku is not null);
    }
}
