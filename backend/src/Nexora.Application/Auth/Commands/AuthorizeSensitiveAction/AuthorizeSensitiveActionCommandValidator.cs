using FluentValidation;

namespace Nexora.Application.Auth.Commands.AuthorizeSensitiveAction;

/// <summary>Forma do corpo — porta de authorizeRequestSchema (packages/contracts/src/auth.ts).</summary>
public sealed class AuthorizeSensitiveActionCommandValidator : AbstractValidator<AuthorizeSensitiveActionCommand>
{
    public AuthorizeSensitiveActionCommandValidator()
    {
        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Informe a ação a ser autorizada.")
            .Matches(@"^[A-Z][A-Z0-9_]{2,63}$").WithMessage("Ação inválida.");

        RuleFor(x => x.Pin)
            .NotEmpty().WithMessage("Informe o PIN de quem está autorizando.")
            .Matches(@"^\d{4,6}$").WithMessage("O PIN deve ter de 4 a 6 dígitos.");

        RuleFor(x => x.Context)
            .NotNull().WithMessage("Informe o contexto da autorização.");
    }
}
