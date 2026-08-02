using FluentValidation;

namespace Nexora.Application.Catalog.Modifiers.Commands.CreateModifier;

public sealed class CreateModifierCommandValidator : AbstractValidator<CreateModifierCommand>
{
    public CreateModifierCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty().WithMessage("O grupo de modificadores é obrigatório.");

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Informe um nome para o modificador.")
            .Must(value => value.Trim().Length <= 100).WithMessage("Nome do modificador deve ter até 100 caracteres.");

        RuleFor(x => x.PriceDelta)
            .PrecisionScale(12, 2, ignoreTrailingZeros: false)
            .WithMessage("O preço deve ter no máximo 10 dígitos inteiros e 2 casas decimais.");

        RuleFor(x => x.Quantity)
            .PrecisionScale(14, 4, ignoreTrailingZeros: false).When(x => x.Quantity is not null)
            .WithMessage("A quantidade deve ter no máximo 10 dígitos inteiros e 4 casas decimais.")
            .GreaterThanOrEqualTo(0m).When(x => x.Quantity is not null)
            .WithMessage("A quantidade de insumo não pode ser negativa.");
    }
}
