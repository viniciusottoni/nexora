using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Alerts;

namespace Nexora.Application.Alerts.Queries.GetAlertRouting;

/// <summary>US-082 §7 <c>GET /v1/tenant/alert-routing</c> — matriz totalmente resolvida (override do tenant, senão o padrão do tipo).</summary>
public sealed record GetAlertRoutingQuery : IQuery<IReadOnlyDictionary<string, AlertRoutingRuleResponse>>;
