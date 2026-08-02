using FluentValidation;

namespace Nexora.Application.Installations.Commands.ConsumeInstallationToken;

public sealed class ConsumeInstallationTokenCommandValidator : AbstractValidator<ConsumeInstallationTokenCommand>
{
    public ConsumeInstallationTokenCommandValidator()
    {
        RuleFor(x => x.RawToken)
            .NotEmpty().WithMessage("Informe o token de instalação.")
            .MinimumLength(16).WithMessage("Token de instalação inválido.");
    }
}
