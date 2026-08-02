using FluentValidation;

namespace Awaken.Application.Auth.Commands.Register;

public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    private static readonly string[] SupportedLanguages = ["pt-BR", "en", "es", "fr"];

    public RegisterUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Language)
            .Must(lang => SupportedLanguages.Contains(lang))
            .WithMessage("Language must be one of: pt-BR, en, es, fr");
    }
}
