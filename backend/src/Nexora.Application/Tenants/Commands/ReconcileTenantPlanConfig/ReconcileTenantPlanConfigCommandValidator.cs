using FluentValidation;

namespace Nexora.Application.Tenants.Commands.ReconcileTenantPlanConfig;

public sealed class ReconcileTenantPlanConfigCommandValidator : AbstractValidator<ReconcileTenantPlanConfigCommand>
{
    public ReconcileTenantPlanConfigCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
