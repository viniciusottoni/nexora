using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Alerts;

namespace Nexora.Application.Alerts.Queries.GetAlerts;

/// <summary>
/// US-080 §7 <c>GET /v1/alerts?status=open</c> e US-081 §7 <c>GET /v1/notifications?status=unread</c>
/// (mesma consulta; <paramref name="OnlyForCurrentUser"/> restringe ao usuário autenticado — sua
/// central de notificações, US-081 §3).
/// </summary>
public sealed record GetOpenAlertsQuery(bool OnlyForCurrentUser = false, int Limit = 100) : IQuery<AlertListResponse>;
