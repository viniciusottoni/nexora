using FluentValidation;

namespace Nexora.Application.Tenants.Commands.UpdateTenantPlan;

/// <summary>
/// Validação de FORMA apenas — mesma convenção de <c>TransitionTenantStatusCommandValidator</c>:
/// motivo vazio (422 REASON_REQUIRED) e código de plano inexistente/desativado (422
/// PLAN_NOT_AVAILABLE) são checados no handler, que precisa consultar o banco para isso.
/// </summary>
public sealed class UpdateTenantPlanCommandValidator : AbstractValidator<UpdateTenantPlanCommand>
{
    public UpdateTenantPlanCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Plan).NotEmpty();
    }
}
