using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Tenants;

namespace Nexora.Application.Tenants.Queries.ListTenants;

/// <summary>Porta de <c>GET /v1/platform/tenants</c> — sem paginação por cursor ainda, mesmo escopo do NestJS original.</summary>
public sealed record ListTenantsQuery : IQuery<TenantListResponse>;
