using FluentValidation;

namespace Nexora.Application.Auth.Commands.RefreshToken;

/// <summary>Forma do corpo — porta de refreshRequestSchema (packages/contracts/src/auth.ts).</summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Informe o refresh token.")
            .MinimumLength(32).WithMessage("Refresh token inválido.");
    }
}
