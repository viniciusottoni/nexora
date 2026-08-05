using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.TenantDomains.Queries.ListTenantDomains;

/// <summary>
/// Porta de <c>GET /v1/platform/domains</c> (US-143 §7) — <paramref name="TenantId"/> nulo lista
/// TODOS os tenants (visão de plataforma); informado, escopa a um único tenant.
/// </summary>
public sealed record ListTenantDomainsQuery(Guid? TenantId) : IQuery<TenantDomainListResponse>;
