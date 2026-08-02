using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Tenants;

namespace Nexora.Application.Tenants.Queries.GetTenantById;

/// <summary>Porta de <c>GET /v1/platform/tenants/:id</c>.</summary>
public sealed record GetTenantByIdQuery(Guid TenantId) : IQuery<TenantSummaryResponse>;
