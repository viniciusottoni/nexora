using FluentValidation;

namespace Awaken.Application.Users.Commands.SelectAvatar;

public class SelectAvatarCommandValidator : AbstractValidator<SelectAvatarCommand>
{
    public SelectAvatarCommandValidator()
    {
        RuleFor(x => x.AvatarKey).NotEmpty().MaximumLength(64);
    }
}
