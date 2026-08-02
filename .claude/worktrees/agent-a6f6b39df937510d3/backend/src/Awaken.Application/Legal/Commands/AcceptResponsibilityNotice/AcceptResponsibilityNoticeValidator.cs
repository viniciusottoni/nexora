using FluentValidation;

namespace Awaken.Application.Legal.Commands.AcceptResponsibilityNotice;

public class AcceptResponsibilityNoticeValidator : AbstractValidator<AcceptResponsibilityNoticeCommand>
{
    public AcceptResponsibilityNoticeValidator()
    {
        RuleFor(x => x.NoticeVersion)
            .NotEmpty()
            .MaximumLength(20);
    }
}
