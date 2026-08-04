using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Alerts;

namespace Nexora.Application.Alerts.Commands.UpdateAlertRouting;

/// <summary>
/// US-082 §7 <c>PATCH /v1/tenant/alert-routing</c> — chave é o tipo de alerta (ex.: <c>ORDER_LATE</c>),
/// valor é um patch parcial (US-083 §7: pode trazer só <c>groupWindowSeconds</c>). Autoridade do
/// dado é a nuvem, mesmo racional de <see cref="Nexora.Application.Alerts.Commands.UpdateTenantThresholds.UpdateTenantThresholdsCommand"/>.
/// </summary>
public sealed record UpdateAlertRoutingCommand(IReadOnlyDictionary<string, AlertRoutingRulePatch> Patch)
    : ICommand<IReadOnlyDictionary<string, AlertRoutingRuleResponse>>;
