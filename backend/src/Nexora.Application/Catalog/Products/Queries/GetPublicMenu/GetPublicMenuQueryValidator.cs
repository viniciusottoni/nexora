using Nexora.Application.Branding;
using FluentValidation;

namespace Nexora.Application.Catalog.Products.Queries.GetPublicMenu;

public sealed class GetPublicMenuQueryValidator : AbstractValidator<GetPublicMenuQuery>
{
    public GetPublicMenuQueryValidator()
    {
        RuleFor(x => x.Host)
            .NotEmpty().WithMessage("Host inválido.")
            .Must(BrandingHost.IsValid).WithMessage("Host inválido.");
    }
}
