using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Alerts;

namespace Nexora.Application.Alerts.Queries.GetTenantThresholds;

/// <summary>US-080 §7 <c>GET /v1/tenant/thresholds</c>.</summary>
public sealed record GetTenantThresholdsQuery : IQuery<TenantThresholdsResponse>;
