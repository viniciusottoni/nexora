using FluentValidation;

namespace Nexora.Application.Tenants.Commands.TransferTenantOwnership;

/// <summary>Validação de FORMA apenas — <see cref="TransferTenantOwnershipCommand.Reason"/> vazio vira 422 <c>REASON_REQUIRED</c> no handler.</summary>
public sealed class TransferTenantOwnershipCommandValidator : AbstractValidator<TransferTenantOwnershipCommand>
{
    public TransferTenantOwnershipCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.NewOwnerUserId).NotEmpty();
    }
}
