using FluentValidation;

namespace Nexora.Application.Auth.Commands.LoginWithPassword;

/// <summary>Forma do corpo — porta de passwordLoginSchema (packages/contracts/src/auth.ts).</summary>
public sealed class LoginWithPasswordCommandValidator : AbstractValidator<LoginWithPasswordCommand>
{
    public LoginWithPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Informe o e-mail.")
            .EmailAddress().WithMessage("Informe um e-mail válido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Informe a senha.")
            .MinimumLength(8).WithMessage("A senha deve ter no mínimo 8 caracteres.")
            .MaximumLength(128).WithMessage("A senha deve ter no máximo 128 caracteres.");

        RuleFor(x => x.Otp)
            .Matches(@"^\d{6}$").WithMessage("O código de verificação deve ter 6 dígitos.")
            .When(x => !string.IsNullOrWhiteSpace(x.Otp));
    }
}
