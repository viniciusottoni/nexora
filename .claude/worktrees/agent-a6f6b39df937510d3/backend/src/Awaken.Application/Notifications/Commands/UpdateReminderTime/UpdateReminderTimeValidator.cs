using FluentValidation;

namespace Awaken.Application.Notifications.Commands.UpdateReminderTime;

public class UpdateReminderTimeValidator : AbstractValidator<UpdateReminderTimeCommand>
{
    public UpdateReminderTimeValidator()
    {
        RuleFor(x => x.Timezone)
            .NotEmpty()
            .WithMessage("Timezone is required.")
            .MaximumLength(100)
            .WithMessage("Timezone must not exceed 100 characters.")
            .Must(tz => tz == null || tz == tz.Trim())
            .WithMessage("Timezone must not have leading or trailing whitespace.");
    }
}
