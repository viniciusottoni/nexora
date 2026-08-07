using FluentValidation;

namespace Nexora.Application.Tenants.Commands.RevokeOwnerInvite;

/// <summary>Validação de FORMA apenas — <see cref="RevokeOwnerInviteCommand.Reason"/> vazio vira 422 <c>REASON_REQUIRED</c> no handler, mesma convenção de <c>TransitionTenantStatusCommandValidator</c>.</summary>
public sealed class RevokeOwnerInviteCommandValidator : AbstractValidator<RevokeOwnerInviteCommand>
{
    public RevokeOwnerInviteCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.InviteId).NotEmpty();
    }
}
