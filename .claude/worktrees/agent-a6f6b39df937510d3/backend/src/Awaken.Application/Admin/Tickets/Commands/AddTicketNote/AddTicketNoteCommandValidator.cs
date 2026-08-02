using FluentValidation;

namespace Awaken.Application.Admin.Tickets.Commands.AddTicketNote;

public class AddTicketNoteCommandValidator : AbstractValidator<AddTicketNoteCommand>
{
    public AddTicketNoteCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();

        RuleFor(x => x.Note)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
