using FluentValidation;

namespace Awaken.Application.Auth.Commands.GoogleSignIn;

public class GoogleSignInValidator : AbstractValidator<GoogleSignInCommand>
{
    public GoogleSignInValidator()
    {
        RuleFor(x => x.Provider)
            .Equal("google")
            .WithMessage("Provider must be google.");

        RuleFor(x => x.ProviderCredential)
            .NotEmpty();
    }
}
