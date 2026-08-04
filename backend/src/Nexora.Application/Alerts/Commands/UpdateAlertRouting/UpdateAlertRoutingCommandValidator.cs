using FluentValidation;
using Nexora.Domain.Metrics;

namespace Nexora.Application.Alerts.Commands.UpdateAlertRouting;

internal sealed class UpdateAlertRoutingCommandValidator : AbstractValidator<UpdateAlertRoutingCommand>
{
    private static readonly HashSet<string> ValidScopes = new(StringComparer.Ordinal)
    {
        Support.AlertRoutingScopes.Tenant, Support.AlertRoutingScopes.Responsible,
        Support.AlertRoutingScopes.TableOwner, Support.AlertRoutingScopes.Station,
    };

    public UpdateAlertRoutingCommandValidator()
    {
        RuleFor(c => c.Patch).NotEmpty().WithMessage("Informe ao menos um tipo de alerta para atualizar.");

        RuleForEach(c => c.Patch.Keys)
            .Must(type => AlertTypes.EngineTypes.Contains(type))
            .WithMessage("Tipo de alerta desconhecido.");

        RuleForEach(c => c.Patch.Values)
            .Must(patch => patch.Scope is null || ValidScopes.Contains(patch.Scope))
            .WithMessage("Escopo inválido — use TENANT, RESPONSIBLE, TABLE_OWNER ou STATION.");

        RuleForEach(c => c.Patch.Values)
            .Must(patch => patch.EscalateAfterSeconds is null or > 0)
            .WithMessage("O prazo de escalonamento deve ser maior que zero.");

        RuleForEach(c => c.Patch.Values)
            .Must(patch => patch.GroupWindowSeconds is null or > 0)
            .WithMessage("A janela de agrupamento deve ser maior que zero.");
    }
}
