using FluentValidation;

namespace Nexora.Application.Catalog.ProductModifierGroups.Commands.UnlinkModifierGroupFromProduct;

public sealed class UnlinkModifierGroupFromProductCommandValidator : AbstractValidator<UnlinkModifierGroupFromProductCommand>
{
    public UnlinkModifierGroupFromProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("O produto é obrigatório.");
        RuleFor(x => x.GroupId).NotEmpty().WithMessage("O grupo de modificadores é obrigatório.");
    }
}
