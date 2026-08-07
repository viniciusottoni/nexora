using FluentValidation;

namespace Nexora.Application.Platform.Queries.GetAttentionQueue;

/// <summary>Mesmo teto de <c>ListTenantsQueryValidator</c> (US-151): limite entre 1 e 100.</summary>
public sealed class GetAttentionQueueQueryValidator : AbstractValidator<GetAttentionQueueQuery>
{
    public GetAttentionQueueQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100).WithMessage("O limite deve estar entre 1 e 100.");
    }
}
