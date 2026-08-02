using FluentValidation;

namespace Nexora.Application.Catalog.ProductModifierGroups.Commands.LinkModifierGroupToProduct;

public sealed class LinkModifierGroupToProductCommandValidator : AbstractValidator<LinkModifierGroupToProductCommand>
{
    public LinkModifierGroupToProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("O produto é obrigatório.");
        RuleFor(x => x.GroupId).NotEmpty().WithMessage("O grupo de modificadores é obrigatório.");
    }
}
