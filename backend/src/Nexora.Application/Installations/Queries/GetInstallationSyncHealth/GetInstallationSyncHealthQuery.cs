using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Installations;

namespace Nexora.Application.Installations.Queries.GetInstallationSyncHealth;

/// <summary>GET /v1/sync/health (cloud, autenticado) — porta de <c>InitialSyncController.health</c>.</summary>
public sealed record GetInstallationSyncHealthQuery(Guid TenantId, Guid InstallationId) : IQuery<InstallationSyncHealthResponse>;
