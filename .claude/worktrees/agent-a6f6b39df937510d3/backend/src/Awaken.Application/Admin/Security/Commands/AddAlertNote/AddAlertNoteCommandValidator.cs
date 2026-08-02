using FluentValidation;

namespace Awaken.Application.Admin.Security.Commands.AddAlertNote;

public class AddAlertNoteCommandValidator : AbstractValidator<AddAlertNoteCommand>
{
    public AddAlertNoteCommandValidator()
    {
        RuleFor(x => x.Note)
            .NotEmpty()
            .MaximumLength(500);
    }
}
