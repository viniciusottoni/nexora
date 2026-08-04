using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Alerts;

namespace Nexora.Application.Alerts.Queries.GetAlerts;

/// <summary>US-083 §7 <c>GET /v1/alerts?grouped=true</c>.</summary>
public sealed record GetGroupedAlertsQuery : IQuery<AlertGroupListResponse>;
