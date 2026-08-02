using FluentValidation;

namespace Nexora.Application.Tenants.Commands.AcceptOwnerInvitation;

public sealed class AcceptOwnerInvitationCommandValidator : AbstractValidator<AcceptOwnerInvitationCommand>
{
    public AcceptOwnerInvitationCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Informe o token do convite.")
            .MinimumLength(32).WithMessage("Token de convite inválido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Informe uma senha.")
            .MinimumLength(12).WithMessage("A senha deve ter pelo menos 12 caracteres.");
    }
}
