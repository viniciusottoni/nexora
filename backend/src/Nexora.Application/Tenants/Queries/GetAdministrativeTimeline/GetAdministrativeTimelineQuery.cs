using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Tenants.Support;
using Nexora.Contracts.Tenants;

namespace Nexora.Application.Tenants.Queries.GetAdministrativeTimeline;

/// <summary>US-157 §"Contrato de API" — <c>GET /v1/platform/tenants/{id}/administrative-timeline?type=STATUS,PLAN&amp;from=...&amp;to=...</c>.</summary>
public sealed record GetAdministrativeTimelineQuery(
    Guid TenantId,
    IReadOnlyCollection<AdministrativeTimelineEntryType> Type,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Limit,
    string? Cursor,
    Guid? ActorId = null,
    string? CorrelationId = null) : IQuery<AdministrativeTimelineListResponse>;
