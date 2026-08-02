using FluentValidation;

namespace Awaken.Application.Admin.Bugs.Commands.UpdateOperationalBug;

/// <summary>
/// US-164: quando Status é informado, deve pertencer ao domínio permitido.
/// </summary>
public class UpdateOperationalBugCommandValidator : AbstractValidator<UpdateOperationalBugCommand>
{
    private static readonly string[] ValidStatuses = ["open", "in_progress", "resolved", "closed"];

    public UpdateOperationalBugCommandValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Contains(s!.ToLowerInvariant()))
            .When(x => x.Status is not null)
            .WithMessage("Status must be one of: open, in_progress, resolved, closed");
    }
}
