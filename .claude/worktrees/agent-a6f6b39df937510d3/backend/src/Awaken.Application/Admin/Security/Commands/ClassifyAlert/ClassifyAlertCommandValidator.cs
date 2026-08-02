using FluentValidation;

namespace Awaken.Application.Admin.Security.Commands.ClassifyAlert;

/// <summary>US-219 RN-005: Classification deve pertencer ao domínio permitido.</summary>
public class ClassifyAlertCommandValidator : AbstractValidator<ClassifyAlertCommand>
{
    private static readonly string[] ValidClassifications = ["false_positive", "confirmed"];

    public ClassifyAlertCommandValidator()
    {
        RuleFor(x => x.Classification)
            .NotEmpty()
            .Must(c => ValidClassifications.Contains(c.ToLowerInvariant()))
            .WithMessage("Classification must be one of: false_positive, confirmed");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => x.Note is not null);
    }
}
