using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Alerts.Commands.EscalatePendingAlerts;

/// <summary>
/// US-082 §4 "Escalonamento por falta de resposta": alerta sem reconhecimento além de
/// <c>escalateAfterSeconds</c> (matriz de direcionamento, US-082 §7) escala para o gestor. Reusado
/// por Edge (alertas operacionais) e Cloud (alertas de gestão) — <c>TenantId</c> explícito nos dois
/// hosts, mesmo padrão de <c>EvaluateCloudAlertConditionsCommand</c>.
/// </summary>
public sealed record EscalatePendingAlertsCommand(Guid TenantId) : ICommand<int>;
