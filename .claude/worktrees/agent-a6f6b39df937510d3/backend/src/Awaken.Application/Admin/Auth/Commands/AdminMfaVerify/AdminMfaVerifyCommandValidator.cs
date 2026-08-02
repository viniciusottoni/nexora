using FluentValidation;

namespace Awaken.Application.Admin.Auth.Commands.AdminMfaVerify;

public class AdminMfaVerifyCommandValidator : AbstractValidator<AdminMfaVerifyCommand>
{
    public AdminMfaVerifyCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches("^[0-9]{6}$")
            .WithMessage("Code must be exactly 6 digits.");
    }
}
