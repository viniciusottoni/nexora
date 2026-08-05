using FluentValidation;

namespace Nexora.Application.Tenants.Commands.TransitionTenantStatus;

/// <summary>
/// Validação de FORMA apenas — <see cref="TransitionTenantStatusCommand.Reason"/> vazio e status de
/// destino inválido/não permitido são checados no handler, que devolve os códigos específicos do
/// contrato (422 REASON_REQUIRED, 409 TENANT_STATUS_TRANSITION_INVALID) em vez do 400
/// VALIDATION_FAILED genérico que este validator produziria.
/// </summary>
public sealed class TransitionTenantStatusCommandValidator : AbstractValidator<TransitionTenantStatusCommand>
{
    public TransitionTenantStatusCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.TargetStatus).NotEmpty();
    }
}
