using FluentValidation;

namespace Nexora.Application.Tenants.Commands.UnlockOwnerAccess;

/// <summary>Validação de FORMA apenas — <see cref="UnlockOwnerAccessCommand.Reason"/> vazio vira 422 <c>REASON_REQUIRED</c> no handler.</summary>
public sealed class UnlockOwnerAccessCommandValidator : AbstractValidator<UnlockOwnerAccessCommand>
{
    public UnlockOwnerAccessCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
