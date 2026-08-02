using FluentValidation;

namespace Nexora.Application.Tenants.Queries.CheckTenantSlugAvailability;

public sealed class CheckTenantSlugAvailabilityQueryValidator : AbstractValidator<CheckTenantSlugAvailabilityQuery>
{
    public CheckTenantSlugAvailabilityQueryValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Informe um endereço válido.")
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Informe um endereço válido.")
            .MaximumLength(63).WithMessage("Informe um endereço válido.");
    }
}
