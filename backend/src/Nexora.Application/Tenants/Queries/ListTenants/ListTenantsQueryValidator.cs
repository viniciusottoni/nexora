using FluentValidation;

namespace Nexora.Application.Tenants.Queries.ListTenants;

/// <summary>US-151 §7 "limit — default 25, máximo 100" — mesmo padrão de <c>CheckTenantSlugAvailabilityQueryValidator</c>.</summary>
public sealed class ListTenantsQueryValidator : AbstractValidator<ListTenantsQuery>
{
    public ListTenantsQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100).WithMessage("O limite deve estar entre 1 e 100.");

        RuleFor(x => x.CreatedFrom)
            .LessThanOrEqualTo(x => x.CreatedTo)
            .When(x => x.CreatedFrom is not null && x.CreatedTo is not null)
            .WithMessage("O início do período de criação não pode ser depois do fim.");
    }
}
