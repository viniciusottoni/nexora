using FluentValidation;

namespace Awaken.Application.Notifications.Commands.UpdatePreferences;

public class UpdateNotificationPreferencesValidator : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    private static readonly string[] ValidPermissionStatuses =
    [
        "not_requested",
        "requesting",
        "granted",
        "denied",
        "token_registered",
        "sync_error"
    ];

    public UpdateNotificationPreferencesValidator()
    {
        RuleFor(x => x.PushToken)
            .NotEmpty()
            .When(x => x.PushEnabled && x.PushToken is not null)
            .WithMessage("PushToken cannot be empty when provided.")
            .MaximumLength(512)
            .When(x => x.PushToken is not null);

        RuleFor(x => x.PermissionStatus)
            .Must(status => status is null || ValidPermissionStatuses.Contains(status))
            .WithMessage($"PermissionStatus must be one of: {string.Join(", ", ValidPermissionStatuses)}");
    }
}
