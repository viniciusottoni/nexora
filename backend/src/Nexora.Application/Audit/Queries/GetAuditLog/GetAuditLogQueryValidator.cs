using FluentValidation;

namespace Nexora.Application.Audit.Queries.GetAuditLog;

public sealed class GetAuditLogQueryValidator : AbstractValidator<GetAuditLogQuery>
{
    public const int MaxLimit = 200;

    public GetAuditLogQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, MaxLimit)
            .WithMessage($"O limite deve estar entre 1 e {MaxLimit}.");

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("O fim do período deve ser posterior ao início.");

        RuleFor(x => x.MinAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinAmount is not null)
            .WithMessage("O valor mínimo não pode ser negativo.");
    }
}
