using Nexora.Application.Branding;
using FluentValidation;

namespace Nexora.Application.Branding.Queries.GetBrandingManifest;

public sealed class GetBrandingManifestQueryValidator : AbstractValidator<GetBrandingManifestQuery>
{
    public GetBrandingManifestQueryValidator()
    {
        RuleFor(x => x.Host)
            .NotEmpty().WithMessage("Host inválido.")
            .Must(BrandingHost.IsValid).WithMessage("Host inválido.");
    }
}
