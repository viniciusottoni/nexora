using Nexora.Application.Branding;
using FluentValidation;

namespace Nexora.Application.Branding.Queries.GetPublicBranding;

public sealed class GetPublicBrandingQueryValidator : AbstractValidator<GetPublicBrandingQuery>
{
    public GetPublicBrandingQueryValidator()
    {
        RuleFor(x => x.Host)
            .NotEmpty().WithMessage("Host inválido.")
            .Must(BrandingHost.IsValid).WithMessage("Host inválido.");
    }
}
