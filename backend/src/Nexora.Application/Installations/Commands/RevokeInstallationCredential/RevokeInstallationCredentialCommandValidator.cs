using FluentValidation;

namespace Nexora.Application.Installations.Commands.RevokeInstallationCredential;

public sealed class RevokeInstallationCredentialCommandValidator : AbstractValidator<RevokeInstallationCredentialCommand>
{
    public RevokeInstallationCredentialCommandValidator()
    {
        RuleFor(x => x.InstallationId).NotEmpty().WithMessage("O id da instalação é obrigatório.");
        RuleFor(x => x.CredentialId).NotEmpty().WithMessage("O id da credencial é obrigatório.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("O motivo da revogação é obrigatório.")
            .MaximumLength(500).WithMessage("O motivo da revogação deve ter no máximo 500 caracteres.");
    }
}
