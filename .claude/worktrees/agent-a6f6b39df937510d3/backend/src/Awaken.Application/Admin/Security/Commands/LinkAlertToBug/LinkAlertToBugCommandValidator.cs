using FluentValidation;

namespace Awaken.Application.Admin.Security.Commands.LinkAlertToBug;

public class LinkAlertToBugCommandValidator : AbstractValidator<LinkAlertToBugCommand>
{
    public LinkAlertToBugCommandValidator()
    {
        RuleFor(x => x.BugId).NotEmpty();
    }
}
