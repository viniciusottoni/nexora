using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Tenants;

namespace Nexora.Application.Tenants.Queries.GetTenantDeploymentStatus;

/// <summary>Porta de <c>GET /v1/platform/tenants/{tenantId}/deployment</c> (US-156, exclusiva da policy <c>PlatformAdmin</c>).</summary>
public sealed record GetTenantDeploymentStatusQuery(Guid TenantId) : IQuery<TenantDeploymentStatusResponse>;
