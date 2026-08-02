using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Tenants;

namespace Nexora.Application.Tenants.Queries.CheckTenantSlugAvailability;

/// <summary>Porta de <c>GET /v1/platform/tenants/slug-availability</c>.</summary>
public sealed record CheckTenantSlugAvailabilityQuery(string Slug) : IQuery<SlugAvailabilityResponse>;
