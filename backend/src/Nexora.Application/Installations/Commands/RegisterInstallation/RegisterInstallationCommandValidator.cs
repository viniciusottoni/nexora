using FluentValidation;

namespace Nexora.Application.Installations.Commands.RegisterInstallation;

public sealed class RegisterInstallationCommandValidator : AbstractValidator<RegisterInstallationCommand>
{
    public RegisterInstallationCommandValidator()
    {
        RuleFor(x => x.RawToken)
            .NotEmpty().WithMessage("Informe o token de instalação.")
            .MinimumLength(16).WithMessage("Token de instalação inválido.");

        RuleFor(x => x.InstallationId).NotEmpty().WithMessage("O id da instalação é obrigatório.");

        RuleFor(x => x.Hostname)
            .NotEmpty().WithMessage("O hostname do edge é obrigatório.")
            .MaximumLength(255).WithMessage("O hostname do edge deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Version)
            .NotEmpty().WithMessage("A versão do edge é obrigatória.")
            .MaximumLength(20).WithMessage("A versão do edge deve ter no máximo 20 caracteres.");

        RuleFor(x => x.PublicKey)
            .NotEmpty().WithMessage("A chave pública do edge é obrigatória.")
            .MinimumLength(32).WithMessage("A chave pública do edge é inválida.")
            .MaximumLength(4096).WithMessage("A chave pública do edge é inválida.");
    }
}
