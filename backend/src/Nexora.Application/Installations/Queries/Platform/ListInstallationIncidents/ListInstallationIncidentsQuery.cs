using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.Installations.Queries.Platform.ListInstallationIncidents;

/// <summary>US-140 §7 — <c>GET /v1/platform/installations/{id}/incidents</c> (histórico, mais recente primeiro).</summary>
public sealed record ListInstallationIncidentsQuery(Guid InstallationId) : IQuery<InstallationIncidentListResponse>;
