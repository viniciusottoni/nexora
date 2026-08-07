using FluentValidation;

namespace Nexora.Application.Tenants.Commands.ReissueOwnerInvite;

/// <summary>
/// Validação de FORMA apenas — <see cref="ReissueOwnerInviteCommand.Reason"/> vazio é checado no
/// handler (devolve 422 <c>REASON_REQUIRED</c>, mesmo código/convenção de
/// <c>TransitionTenantStatusCommandValidator</c>) em vez do 400 genérico que esta regra produziria.
/// </summary>
public sealed class ReissueOwnerInviteCommandValidator : AbstractValidator<ReissueOwnerInviteCommand>
{
    public ReissueOwnerInviteCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("Informe o nome do proprietário.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Informe um e-mail válido.");
    }
}
