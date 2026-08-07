using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Tenants;

namespace Nexora.Application.Tenants.Queries.GetTenantOverview;

/// <summary>Porta de <c>GET /v1/platform/tenants/{id}/overview</c> (US-152, exclusiva da policy <c>PlatformAdmin</c>).</summary>
public sealed record GetTenantOverviewQuery(Guid TenantId) : IQuery<TenantOverviewResponse>;
