using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Tenants;

namespace Nexora.Application.Tenants.Queries.GetTenantOwnership;

/// <summary>
/// US-155 · Proprietários, usuários iniciais e convites — porta de
/// <c>GET /v1/platform/tenants/{id}/ownership</c> (exclusiva da policy <c>PlatformAdmin</c>).
/// </summary>
public sealed record GetTenantOwnershipQuery(Guid TenantId) : IQuery<TenantOwnershipResponse>;
