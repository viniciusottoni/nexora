using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Alerts.Commands.EvaluateEdgeAlertConditions;

/// <summary>
/// US-080 §9 "o motor roda no edge para os alertas operacionais" — avalia, numa única passada,
/// todo o subconjunto do catálogo do MVP que depende só do estado local: <c>ORDER_LATE</c>,
/// <c>AVG_TIME_ABOVE_TARGET</c>, <c>CANCELLATION_ABOVE_THRESHOLD</c>, <c>DISCOUNT_ABOVE_THRESHOLD</c>.
/// Sem parâmetro de tenant — o edge tem exatamente um (ADR-004), lido de <c>ICurrentTenantContext</c>
/// mesmo fora de uma requisição HTTP (mesma decisão documentada em <c>WaiterCallEscalationWorker</c>).
/// </summary>
public sealed record EvaluateEdgeAlertConditionsCommand : ICommand<int>;
