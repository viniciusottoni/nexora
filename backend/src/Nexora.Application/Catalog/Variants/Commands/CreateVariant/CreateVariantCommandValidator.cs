using Nexora.Domain.Catalog;
using FluentValidation;

namespace Nexora.Application.Catalog.Variants.Commands.CreateVariant;

public sealed class CreateVariantCommandValidator : AbstractValidator<CreateVariantCommand>
{
    public CreateVariantCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Selecione um produto.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Informe um nome.")
            .MaximumLength(150).WithMessage("O nome deve ter no máximo 150 caracteres.");

        RuleFor(x => x.SizeCode)
            .MaximumLength(16).WithMessage("O código de tamanho deve ter no máximo 16 caracteres.")
            .When(x => x.SizeCode is not null);

        RuleFor(x => x.Sku)
            .MaximumLength(40).WithMessage("O SKU deve ter no máximo 40 caracteres.")
            .When(x => x.Sku is not null);

        RuleFor(x => x.PrepMinutes)
            .GreaterThanOrEqualTo((short)0).WithMessage("O tempo de preparo não pode ser negativo.")
            .When(x => x.PrepMinutes is not null);

        RuleFor(x => x.BasePrice)
            .GreaterThanOrEqualTo(0m).WithMessage("O preço não pode ser negativo.")
            .PrecisionScale(12, 2, true).WithMessage("O preço deve ter no máximo duas casas decimais.");

        RuleFor(x => x.Channel)
            .Must(c => Enum.TryParse<Channel>(c, ignoreCase: true, out _))
            .WithMessage("Canal inválido.")
            .When(x => x.Channel is not null);
    }
}
