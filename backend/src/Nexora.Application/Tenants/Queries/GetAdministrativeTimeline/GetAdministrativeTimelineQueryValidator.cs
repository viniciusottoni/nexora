using FluentValidation;

namespace Nexora.Application.Tenants.Queries.GetAdministrativeTimeline;

public sealed class GetAdministrativeTimelineQueryValidator : AbstractValidator<GetAdministrativeTimelineQuery>
{
    public GetAdministrativeTimelineQueryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 200).WithMessage("O limite deve estar entre 1 e 200.");

        RuleFor(x => x.From)
            .LessThanOrEqualTo(x => x.To)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("O início do período não pode ser depois do fim.");
    }
}
