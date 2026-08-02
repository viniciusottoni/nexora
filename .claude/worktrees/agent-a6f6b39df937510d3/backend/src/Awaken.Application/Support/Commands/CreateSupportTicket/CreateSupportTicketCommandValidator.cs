using FluentValidation;

namespace Awaken.Application.Support.Commands.CreateSupportTicket;

public class CreateSupportTicketCommandValidator : AbstractValidator<CreateSupportTicketCommand>
{
    private static readonly string[] ValidCategories = ["report", "question", "suggestion"];

    public CreateSupportTicketCommandValidator()
    {
        RuleFor(x => x.Category)
            .NotEmpty()
            .Must(c => ValidCategories.Contains(c.ToLowerInvariant()))
            .WithMessage("Category must be one of: report, question, suggestion");

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.AppVersion)
            .MaximumLength(32)
            .When(x => x.AppVersion is not null);
    }
}
